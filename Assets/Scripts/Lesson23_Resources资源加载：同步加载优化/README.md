一、为什么要优化异步加载
`Resources` 加载过的资源会保存在内存中。再次加载同一资源时，通常会直接使用已有资源，不会重复占用一份资源内存。
但是每次调用异步加载仍然会进行查找并开启协程，重复调用会产生不必要的性能开销。
==优化目标：==
```text
第一次异步加载 → 开启协程
加载过程中重复请求 → 只记录回调，不开启新协程
资源加载完成后重复请求 → 直接返回缓存资源
```

# 二、资源信息的数据结构
## 1. 资源信息基类
```csharp
public abstract class ResInfoBase
{
}
```
字典需要保存不同资源类型的 `ResInfo<T>`，因此使用共同的父类 `ResInfoBase` 作为字典 Value。
## 2. 泛型资源信息类
```csharp
public class ResInfo<T> : ResInfoBase
{
    public T asset;
    public UnityAction<T> callBack;
    public Coroutine coroutine;
}
```
- `asset`：加载完成后的资源。
- `callBack`：等待该资源的所有回调方法。
- `coroutine`：当前正在执行的加载协程。

# 三、资源缓存字典
```csharp
private Dictionary<string, ResInfoBase> resDic = new();
```
字典同时记录正在加载和已经加载完成的资源。资源的 Key 由“路径 + 类型”组成：
```csharp
string resName = path + "_" + typeof(T).Name;
```
例如：
```text
Test_GameObject
UI/Icon_Sprite
Audio/BGM_AudioClip
```
路径相同但类型不同的资源会得到不同的 Key。

# 四、第一次异步加载
当字典中不存在资源记录时：
```csharp
if (!resDic.ContainsKey(resName))
{
    info = new ResInfo<T>();
    resDic.Add(resName, info);

    info.callBack += callBack;
    info.coroutine = MonoMgr.Instance.StartCoroutine
    (ReallyLoadAsync<T>(path)
    );
}
```
执行顺序：
```text
创建 ResInfo<T>
      ↓
先加入 resDic
      ↓
记录回调
      ↓
开启一个加载协程
```
必须先加入字典，后续相同请求才能发现该资源正在加载，避免再次开启协程。

# 五、重复请求同一资源
字典中已经存在记录时，需要判断资源是否加载完成：
```csharp
info = resDic[resName] as ResInfo<T>;
if (info.asset == null)
{
    info.callBack += callBack;
}
else
{
    callBack?.Invoke(info.asset);
}
```
## 1. 资源还在加载
`asset == null` 表示加载尚未完成。
此时只把新的回调加入委托，不会开启第二个协程：
```csharp
info.callBack += callBack;
```
## 2. 资源已经加载完成
`asset != null` 表示字典中已经有缓存，直接通过回调返回：
```csharp
callBack?.Invoke(info.asset);
```

# 六、异步加载完成后的处理
```csharp
private IEnumerator ReallyLoadAsync<T>(string path) where T : UnityEngine.Object
{
    ResourceRequest request = Resources.LoadAsync<T>(path);
    yield return request;

    string resName = path + "_" + typeof(T).Name;

    if (resDic.ContainsKey(resName))
    {
        ResInfo<T> info = resDic[resName] as ResInfo<T>;
        info.asset = request.asset as T;
        info.callBack?.Invoke(info.asset);
        info.callBack = null;
        info.coroutine = null;
    }
}
```
加载结束后需要完成四件事：
1. 将资源保存到 `asset`。
2. 通知所有等待该资源的调用者。
3. 清空回调引用。
4. 清空协程引用。
资源本身继续保存在 `resDic` 中，后续请求可以直接获取。

# 七、重复加载测试
```csharp
ResMgr.Instance.LoadAsync<GameObject>("Test", obj =>
{
    Instantiate(obj);
});

ResMgr.Instance.LoadAsync<GameObject>("Test", obj =>
{
    Instantiate(obj);
});
```
这两次调用会产生：
```text
一个 ResourceRequest
一个加载协程
两个回调
两个场景实例
```
加载的资源只有一份，但两个回调都会执行，所以会调用两次 `Instantiate()`。

# 八、Type 加载方式的问题
代码仍然保留了 `Type` 异步加载方式，但已经使用 `[Obsolete]` 标记：
```csharp
[Obsolete("建议使用泛型加载方式")]
```
泛型方式保存的是：
```csharp
ResInfo<GameObject>
```
`Type` 方式保存的是：
```csharp
ResInfo<UnityEngine.Object>
```
两种方式加载同路径、同类型资源时会得到相同 Key，但对应的 `ResInfo` 泛型类型不同，可能导致类型转换失败。因此同一资源不能混用两种加载方式，建议统一使用泛型方式。

# 九、Resources 卸载知识
## 1. 卸载指定资源
```csharp
Resources.UnloadAsset(assetToUnload);
```
只能用于不需要实例化的单个资源，例如图片、音效和文本等，不能用来卸载 `GameObject
## 2. 卸载未使用资源
```csharp
Resources.UnloadUnusedAssets();
```
用于异步卸载没有被使用的资源，通常在切换场景时配合垃圾回收使用。

# 十、完整代码
## 1. ResMgr.cs
```
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 资源信息基类。
/// 用于让字典保存不同类型的 ResInfo<T>。
/// </summary>
public abstract class ResInfoBase
{
}

/// <summary>
/// 保存资源及其异步加载信息。
/// </summary>
public class ResInfo<T> : ResInfoBase
{
    // 加载完成的资源
    public T asset;

    // 等待该资源的所有回调
    public UnityAction<T> callBack;

    // 当前执行的加载协程
    public Coroutine coroutine;
}

/// <summary>
/// Resources 资源加载管理器。
/// </summary>
public class ResMgr : BaseManager<ResMgr>
{
    // 保存正在加载和已经加载完成的资源
    private readonly Dictionary<string, ResInfoBase> resDic = new();

    private ResMgr()
    {
    }

    /// <summary>
    /// 同步加载 Resources 资源。
    /// 当前同步加载尚未接入自建缓存字典。
    /// </summary>
    public T Load<T>(string path)
        where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }

    /// <summary>
    /// 泛型异步加载资源。
    /// </summary>
    public void LoadAsync<T>(
        string path,
        UnityAction<T> callBack)
        where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("资源路径不能为空");
            return;
        }

        // 路径和类型共同组成资源的唯一标识
        string resName = path + "_" + typeof(T).Name;

        // 字典中不存在，说明这是第一次请求该资源
        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            ResInfo<T> info = new ResInfo<T>();

            // 必须先加入字典，避免后续请求重复开启协程
            resDic.Add(resName, info);

            // 记录等待资源的回调
            info.callBack += callBack;

            // 开启并记录加载协程
            info.coroutine = MonoMgr.Instance.StartCoroutine(
                ReallyLoadAsync<T>(path)
            );

            return;
        }

        // 取出该资源对应的信息
        ResInfo<T> resInfo = baseInfo as ResInfo<T>;

        if (resInfo == null)
        {
            Debug.LogError(
                $"资源信息类型不匹配：{resName}"
            );
            return;
        }

        // asset 为空，说明资源还在加载
        if (resInfo.asset == null)
        {
            // 只追加回调，不开启新协程
            resInfo.callBack += callBack;
        }
        else
        {
            // 资源已经加载完成，直接返回缓存
            callBack?.Invoke(resInfo.asset);
        }
    }

    /// <summary>
    /// 真正执行泛型异步加载的协程。
    /// </summary>
    private IEnumerator ReallyLoadAsync<T>(string path)
        where T : UnityEngine.Object
    {
        ResourceRequest request =
            Resources.LoadAsync<T>(path);

        yield return request;

        string resName = path + "_" + typeof(T).Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            yield break;
        }

        ResInfo<T> resInfo = baseInfo as ResInfo<T>;

        if (resInfo == null)
        {
            Debug.LogError(
                $"资源信息类型不匹配：{resName}"
            );
            yield break;
        }

        // 保存加载结果
        resInfo.asset = request.asset as T;

        if (resInfo.asset == null)
        {
            Debug.LogError(
                $"Resources 中没有找到资源：{path}"
            );
        }

        // 通知所有等待该资源的调用者
        resInfo.callBack?.Invoke(resInfo.asset);

        // 加载完成后清空不再需要的引用
        resInfo.callBack = null;
        resInfo.coroutine = null;
    }

    /// <summary>
    /// 使用 Type 异步加载资源。
    /// 不建议与泛型方式混合加载同路径、同类型资源。
    /// </summary>
    [Obsolete(
        "建议使用泛型加载方式；Type方式不能与泛型方式混合加载同类型、同名资源"
    )]
    public void LoadAsync(
        string path,
        Type type,
        UnityAction<UnityEngine.Object> callBack)
    {
        if (string.IsNullOrEmpty(path) || type == null)
        {
            Debug.LogError("资源路径或者资源类型不合法");
            return;
        }

        string resName = path + "_" + type.Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            ResInfo<UnityEngine.Object> info =
                new ResInfo<UnityEngine.Object>();

            resDic.Add(resName, info);

            info.callBack += callBack;

            info.coroutine = MonoMgr.Instance.StartCoroutine(
                ReallyLoadAsync(path, type)
            );

            return;
        }

        ResInfo<UnityEngine.Object> resInfo =
            baseInfo as ResInfo<UnityEngine.Object>;

        if (resInfo == null)
        {
            Debug.LogError(
                $"资源信息类型不匹配：{resName}"
            );
            return;
        }

        if (resInfo.asset == null)
        {
            resInfo.callBack += callBack;
        }
        else
        {
            callBack?.Invoke(resInfo.asset);
        }
    }

    /// <summary>
    /// 真正执行 Type 异步加载的协程。
    /// </summary>
    private IEnumerator ReallyLoadAsync(
        string path,
        Type type)
    {
        ResourceRequest request =
            Resources.LoadAsync(path, type);

        yield return request;

        string resName = path + "_" + type.Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            yield break;
        }

        ResInfo<UnityEngine.Object> resInfo =
            baseInfo as ResInfo<UnityEngine.Object>;

        if (resInfo == null)
        {
            Debug.LogError(
                $"资源信息类型不匹配：{resName}"
            );
            yield break;
        }

        resInfo.asset = request.asset;

        if (resInfo.asset == null)
        {
            Debug.LogError(
                $"Resources 中没有找到资源：{path}"
            );
        }

        resInfo.callBack?.Invoke(resInfo.asset);

        resInfo.callBack = null;
        resInfo.coroutine = null;
    }

    /// <summary>
    /// 卸载指定资源。
    /// 当前卸载方法尚未删除 resDic 中的缓存记录。
    /// </summary>
    public void UnloadAsset(
        UnityEngine.Object assetToUnload)
    {
        if (assetToUnload == null)
            return;

        Resources.UnloadAsset(assetToUnload);
    }

    /// <summary>
    /// 异步卸载没有使用的 Resources 资源。
    /// </summary>
    public void UnloadUnusedAssets(
        UnityAction callBack = null)
    {
        MonoMgr.Instance.StartCoroutine(
            ReallyUnloadUnusedAssets(callBack)
        );
    }

    private IEnumerator ReallyUnloadUnusedAssets(
        UnityAction callBack)
    {
        AsyncOperation operation =
            Resources.UnloadUnusedAssets();

        yield return operation;

        callBack?.Invoke();
    }
}
```
## 2. Main.cs
```
using UnityEngine;

public class Main : MonoBehaviour
{
    private void Start()
    {
        // 第一次请求 Test：开启一个异步加载协程
        ResMgr.Instance.LoadAsync<GameObject>(
            "Test",
            prefab =>
            {
                if (prefab != null)
                {
                    Instantiate(prefab);
                    Debug.Log("第一个回调执行");
                }
            }
        );

        // 第二次请求相同资源：
        // 不会开启新协程，只会将回调追加到委托中
        ResMgr.Instance.LoadAsync<GameObject>(
            "Test",
            prefab =>
            {
                if (prefab != null)
                {
                    Instantiate(prefab);
                    Debug.Log("第二个回调执行");
                }
            }
        );
    }
}
```