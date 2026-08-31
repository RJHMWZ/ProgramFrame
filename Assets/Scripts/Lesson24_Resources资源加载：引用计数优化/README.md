# 一、为什么需要引用计数
之前的资源管理器只知道资源是否已经加载，却不知道还有多少地方正在使用它。
如果直接卸载，可能导致其他使用者受到影响；
如果一直不卸载，又会让无用资源长期占用内存。
引用计数用于记录资源当前有多少个使用者：
```text
加载或获取资源 → 引用计数 +1
不再使用资源   → 引用计数 -1
引用计数为 0   → 资源可以被卸载
```

# 二、引用计数的数据结构
引用计数放在所有源信息的父类中：
```csharp
public abstract class ResInfoBase
{
    public int refCount;
}
```
这样 `resDic` 中保存的所有资源信息都具有引用计数。
```csharp
public void AddRefCount()
{
    refCount++;
}

public void SubRefCount()
{
    refCount--;
}
```

# 三、加载资源时增加引用计数
无论同步加载还是异步加载，每次外部请求资源都表示新增一个使用者：
```csharp
info.AddRefCount();
```
例如同一资源被请求三次：
```text
第一次 Load       → refCount = 1
第二次 LoadAsync  → refCount = 2
第三次 Load       → refCount = 3
```
资源本身只加载一份，但引用计数会记录三个使用者。

# 四、不再使用资源时减少引用计数
```csharp
ResMgr.Instance.UnloadAsset<Texture2D>("Textures/Icon");
```
调用卸载方法时，默认先减少一次引用计数：
```csharp
if (isSub)
{
    resInfo.SubRefCount();
}
```
只有加载和卸载配对，引用计数才会准确。
```text
Load / LoadAsync ↔ UnloadAsset
```

# 五、isDel 的作用
`isDel` 表示引用计数归零时，是否马上卸载资源：
```csharp
public bool isDel;
```
卸载方法中的参数：
```csharp
UnloadAsset<T>(string path, bool isDel = false)
```
- `isDel == false`：引用计数归零后仍保留缓存，等待集中清理。
- `isDel == true`：引用计数归零后立即从字典移除并卸载。
立即卸载示例：
```csharp
ResMgr.Instance.UnloadAsset<Texture2D>(
    "Textures/Icon",
    true
);
```

# 六、取消异步请求时移除回调
资源仍在异步加载时，某个调用者可能已经不再需要该资源。除了减少引用计数，还要移除这个调用者的回调：
```csharp
if (callBack != null)
{
    resInfo.callBack -= callBack;
}
```
调用时必须传入注册异步加载时的同一个方法：
```csharp
ResMgr.Instance.LoadAsync<Texture2D>(
    "Textures/Icon",
    LoadIconOver
);

ResMgr.Instance.UnloadAsset<Texture2D>(
    "Textures/Icon",
    false,
    LoadIconOver
);
```
如果使用两个不同的 Lambda，即使内容相同，也无法保证移除的是原来的委托，因此需要取消回调时更适合使用具名方法。

# 七、异步加载完成后的判断
资源加载完成后检查引用计数：
```csharp
if (resInfo.refCount == 0)
{
    UnloadAsset<T>(path, resInfo.isDel, null, false);
}
else
{
    resInfo.callBack?.Invoke(resInfo.asset);
}
```
- `refCount > 0`：仍有使用者，执行剩余回调。
- `refCount == 0`：已经没有使用者，根据 `isDel` 决定是否立即卸载。
- `isSub == false`：这是管理器内部检查，不再重复减少引用计数。

# 八、清理未使用资源
调用 `Resources.UnloadUnusedAssets()` 前，先删除自建字典中引用计数为零的记录：
```csharp
List<string> removeList = new();

foreach (string key in resDic.Keys)
{
    if (resDic[key].refCount == 0)
    {
        removeList.Add(key);
    }
}

foreach (string key in removeList)
{
    resDic.Remove(key);
}
```
不能在遍历字典时直接删除元素，所以先记录 Key，再统一删除。

# 九、辅助功能
获取指定资源的引用计数：
```csharp
int count = ResMgr.Instance.GetRefCount<Texture2D>(
    "Textures/Icon"
);
```
清空管理器的全部资源记录：
```csharp
ResMgr.Instance.ClearDic(() =>
{
    Debug.Log("资源字典清理完成");
});
```

# 十、使用注意事项
1. 每次 `Load()` 或 `LoadAsync()` 都必须对应一次 `UnloadAsset()`。
2. 引用计数是手动维护的，不会自动判断场景中是否真的存在引用。
3. 异步加载取消时，应传入原来注册的回调方法。
4. `isDel = false` 只减少计数并保留缓存；之后可通过 `UnloadUnusedAssets()` 集中清理。
5. `Resources.UnloadAsset()` 不能用于卸载 `GameObject`，场景实例应使用 `Destroy()`。
6. `Type` 加载方式不能与泛型方式混用同路径、同类型资源。

# 十一、完整代码
## ResMgr.cs
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 资源信息基类。
/// </summary>
public abstract class ResInfoBase
{
    // 当前资源的使用者数量
    public int refCount;
}

/// <summary>
/// 保存资源及其加载信息。
/// </summary>
public class ResInfo<T> : ResInfoBase
{
    public T asset;
    public UnityAction<T> callBack;
    public Coroutine coroutine;

    // 引用计数为零时是否立即卸载
    public bool isDel;

    public void AddRefCount()
    {
        refCount++;
    }

    public void SubRefCount()
    {
        if (refCount <= 0)
        {
            Debug.LogError(
                "引用计数不能小于0，请检查加载和卸载是否配对"
            );
            return;
        }

        refCount--;
    }
}

/// <summary>
/// Resources 资源加载管理器。
/// </summary>
public class ResMgr : BaseManager<ResMgr>
{
    private readonly Dictionary<string, ResInfoBase> resDic = new();

    private ResMgr()
    {
    }

    /// <summary>
    /// 同步加载资源。
    /// </summary>
    public T Load<T>(string path)
        where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            T resource = Resources.Load<T>(path);

            ResInfo<T> newInfo = new ResInfo<T>();
            newInfo.asset = resource;
            newInfo.AddRefCount();

            resDic.Add(resName, newInfo);
            return resource;
        }

        ResInfo<T> info = baseInfo as ResInfo<T>;

        if (info == null)
        {
            Debug.LogError($"资源信息类型不匹配：{resName}");
            return null;
        }

        info.AddRefCount();

        // 同一资源正在异步加载，改为同步加载
        if (info.asset == null)
        {
            if (info.coroutine != null)
            {
                MonoMgr.Instance.StopCoroutine(info.coroutine);
            }

            T resource = Resources.Load<T>(path);
            info.asset = resource;

            info.callBack?.Invoke(resource);

            info.callBack = null;
            info.coroutine = null;

            return resource;
        }

        return info.asset;
    }

    /// <summary>
    /// 泛型异步加载资源。
    /// </summary>
    public void LoadAsync<T>(
        string path,
        UnityAction<T> callBack)
        where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            ResInfo<T> info = new ResInfo<T>();
            info.AddRefCount();

            resDic.Add(resName, info);
            info.callBack += callBack;

            info.coroutine = MonoMgr.Instance.StartCoroutine(
                ReallyLoadAsync<T>(path)
            );

            return;
        }

        ResInfo<T> resInfo = baseInfo as ResInfo<T>;

        if (resInfo == null)
        {
            Debug.LogError($"资源信息类型不匹配：{resName}");
            return;
        }

        resInfo.AddRefCount();

        if (resInfo.asset == null)
        {
            resInfo.callBack += callBack;
        }
        else
        {
            callBack?.Invoke(resInfo.asset);
        }
    }

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
            yield break;

        resInfo.asset = request.asset as T;

        if (resInfo.refCount == 0)
        {
            // 管理器内部检查，不重复减少计数
            UnloadAsset<T>(
                path,
                resInfo.isDel,
                null,
                false
            );

            resInfo.callBack = null;
            resInfo.coroutine = null;
        }
        else
        {
            resInfo.callBack?.Invoke(resInfo.asset);
            resInfo.callBack = null;
            resInfo.coroutine = null;
        }
    }

    /// <summary>
    /// Type 异步加载方式。
    /// </summary>
    [Obsolete(
        "建议使用泛型加载；Type方式不能与泛型方式混合加载同类型、同名资源"
    )]
    public void LoadAsync(
        string path,
        Type type,
        UnityAction<UnityEngine.Object> callBack)
    {
        string resName = path + "_" + type.Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            ResInfo<UnityEngine.Object> info =
                new ResInfo<UnityEngine.Object>();

            info.AddRefCount();
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
            Debug.LogError($"资源信息类型不匹配：{resName}");
            return;
        }

        resInfo.AddRefCount();

        if (resInfo.asset == null)
        {
            resInfo.callBack += callBack;
        }
        else
        {
            callBack?.Invoke(resInfo.asset);
        }
    }

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
            yield break;

        resInfo.asset = request.asset;

        if (resInfo.refCount == 0)
        {
            UnloadAsset(
                path,
                type,
                resInfo.isDel,
                null,
                false
            );

            resInfo.callBack = null;
            resInfo.coroutine = null;
        }
        else
        {
            resInfo.callBack?.Invoke(resInfo.asset);
            resInfo.callBack = null;
            resInfo.coroutine = null;
        }
    }

    /// <summary>
    /// 泛型资源卸载。
    /// </summary>
    public void UnloadAsset<T>(
        string path,
        bool isDel = false,
        UnityAction<T> callBack = null,
        bool isSub = true)
        where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            return;
        }

        ResInfo<T> resInfo = baseInfo as ResInfo<T>;

        if (resInfo == null)
            return;

        if (isSub)
        {
            resInfo.SubRefCount();
        }

        // 只要有一次要求立即删除，就保留删除意图
        resInfo.isDel = resInfo.isDel || isDel;

        if (resInfo.asset != null &&
            resInfo.refCount == 0 &&
            resInfo.isDel)
        {
            resDic.Remove(resName);
            Resources.UnloadAsset(resInfo.asset);
        }
        else if (resInfo.asset == null && callBack != null)
        {
            // 调用者不再等待异步结果
            resInfo.callBack -= callBack;
        }
    }

    /// <summary>
    /// Type 资源卸载。
    /// </summary>
    public void UnloadAsset(
        string path,
        Type type,
        bool isDel = false,
        UnityAction<UnityEngine.Object> callBack = null,
        bool isSub = true)
    {
        string resName = path + "_" + type.Name;

        if (!resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            return;
        }

        ResInfo<UnityEngine.Object> resInfo =
            baseInfo as ResInfo<UnityEngine.Object>;

        if (resInfo == null)
            return;

        if (isSub)
        {
            resInfo.SubRefCount();
        }

        resInfo.isDel = resInfo.isDel || isDel;

        if (resInfo.asset != null &&
            resInfo.refCount == 0 &&
            resInfo.isDel)
        {
            resDic.Remove(resName);
            Resources.UnloadAsset(resInfo.asset);
        }
        else if (resInfo.asset == null && callBack != null)
        {
            resInfo.callBack -= callBack;
        }
    }

    /// <summary>
    /// 卸载当前未使用的资源。
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
        List<string> removeList = new List<string>();

        foreach (string key in resDic.Keys)
        {
            if (resDic[key].refCount == 0)
            {
                removeList.Add(key);
            }
        }

        foreach (string key in removeList)
        {
            resDic.Remove(key);
        }

        AsyncOperation operation =
            Resources.UnloadUnusedAssets();

        yield return operation;

        callBack?.Invoke();
    }

    /// <summary>
    /// 获取指定资源的引用计数。
    /// </summary>
    public int GetRefCount<T>(string path)
        where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;

        if (resDic.TryGetValue(
            resName,
            out ResInfoBase baseInfo))
        {
            return baseInfo.refCount;
        }

        return 0;
    }

    /// <summary>
    /// 清空资源字典并卸载未使用资源。
    /// </summary>
    public void ClearDic(UnityAction callBack = null)
    {
        MonoMgr.Instance.StartCoroutine(
            ReallyClearDic(callBack)
        );
    }

    private IEnumerator ReallyClearDic(
        UnityAction callBack)
    {
        resDic.Clear();

        AsyncOperation operation =
            Resources.UnloadUnusedAssets();

        yield return operation;

        callBack?.Invoke();
    }
}
```