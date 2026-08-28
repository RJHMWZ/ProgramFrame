# 一、实现目标
将 `Resources` 的加载和卸载方法统一封装到 `ResMgr` 中：
- 同步加载资源。
- 异步加载资源。
- 卸载指定资源。
- 异步卸载未使用资源。
`ResMgr` 继承非 `MonoBehaviour` 单例基类：
```csharp
public class ResMgr : BaseManager<ResMgr>
```
由于普通 C# 类不能直接开启协程，异步操作需要通过 `MonoMgr` 执行。

# 二、同步加载
```csharp
public T Load<T>(string path) where T : UnityEngine.Object
{
    return Resources.Load<T>(path);
}
```
- `T`：需要加载的资源类型。
- `path`：资源相对于 `Resources` 文件夹的路径。
- 返回值：加载到的资源对象。
调用示例：
```csharp
GameObject prefab = ResMgr.Instance.Load<GameObject>("Test");
GameObject obj = Object.Instantiate(prefab);
```
`Load()` 只负责加载资源，`Instantiate()` 才会在场景中创建实例。

# 三、泛型异步加载
外部调用方法：
```csharp
public void LoadAsync<T>(string path,UnityAction<T> callBack)
    where T : UnityEngine.Object
{
    MonoMgr.Instance.StartCoroutine(ReallyLoadAsync(path, callBack));
}
```
真正执行异步加载的协程：
```csharp
private IEnumerator ReallyLoadAsync<T>(string path,UnityAction<T> callBack)
    where T : UnityEngine.Object
{
    ResourceRequest request = Resources.LoadAsync<T>(path);
    yield return request;
    callBack?.Invoke(request.asset as T);
}
```
执行流程：
```text
调用 LoadAsync<T>()
        ↓
MonoMgr 开启协程
        ↓
Resources.LoadAsync<T>()
        ↓
等待 ResourceRequest完成
        ↓
通过回调返回加载结果
```
调用示例：
```csharp
ResMgr.Instance.LoadAsync<GameObject>("Test", prefab =>
{
    if (prefab != null)
    {
        Instantiate(prefab);
    }
});
```

# 四、Type 异步加载
不知道具体泛型类型，或者类型需要在运行时传入时，可以使用 `Type`：
```csharp
public void LoadAsync(
    string path,
    System.Type type,
    UnityAction<UnityEngine.Object> callBack)
{
    MonoMgr.Instance.StartCoroutine(
        ReallyLoadAsync(path, type, callBack)
    );
}
```

```csharp
private IEnumerator ReallyLoadAsync(
    string path,
    System.Type type,
    UnityAction<UnityEngine.Object> callBack)
{
    ResourceRequest request = Resources.LoadAsync(path, type);

    yield return request;

    callBack?.Invoke(request.asset);
}
```
调用后得到的是 `UnityEngine.Object`，使用前需要转换类型：
```csharp
ResMgr.Instance.LoadAsync(
    "Test",
    typeof(GameObject),
    asset =>
    {
        GameObject prefab = asset as GameObject;

        if (prefab != null)
        {
            Instantiate(prefab);
        }
    }
);
```

# 五、两种异步加载的区别

| 方式 | 回调结果 | 特点 |
|---|---|---|
| `LoadAsync<T>()` | 指定类型 `T` | 类型明确，使用方便 |
| `LoadAsync(path, type)` | `UnityEngine.Object` | 类型可在运行时决定，使用前需要转换 |

# 六、卸载指定资源
```csharp
public void UnloadAsset(UnityEngine.Object assetToUnload)
{
    if (assetToUnload != null)
    {
        Resources.UnloadAsset(assetToUnload);
    }
}
```
`Resources.UnloadAsset()` 用于卸载单个资源，例如纹理、材质或音频资源；不能用它卸载场景实例、`GameObject`、`Component` 或 `AssetBundle`。场景中通过 `Instantiate()` 创建出的实例，应使用：
```csharp
Destroy(gameObject);
```
不能使用 `Resources.UnloadAsset()` 代替实例销毁。

# 七、卸载未使用资源
```csharp
public void UnloadUnusedAssets(UnityAction callBack = null)
{
    MonoMgr.Instance.StartCoroutine(
        ReallyUnloadUnusedAssets(callBack)
    );
}
```
```csharp
private IEnumerator ReallyUnloadUnusedAssets(UnityAction callBack)
{
    AsyncOperation operation = Resources.UnloadUnusedAssets();

    yield return operation;

    callBack?.Invoke();
}
```
执行过程：
```text
Resources.UnloadUnusedAssets()
        ↓
异步清理当前未使用的 Resources 资源
        ↓
清理完成后执行回调
```
该操作不应频繁调用，通常在切换场景或确定需要集中清理资源时使用。

# 八、使用注意事项
1. 资源必须放在名为 `Resources` 的文件夹中。
2. 路径相对于 `Resources` 文件夹，不写文件扩展名。
3. 子文件夹路径使用 `/`，例如：
```csharp
"Prefabs/Player"
```
4. 异步加载结果可能为 `null`，使用前应判空。
5. `ResMgr` 不是 `MonoBehaviour`，必须通过 `MonoMgr` 开启协程。

# 九、完整代码
1. ResMgr.cs
```
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Resources 资源加载管理器。
/// </summary>
public class ResMgr : BaseManager<ResMgr>
{
    private ResMgr()
    {
    }

    /// <summary>
    /// 同步加载资源。
    /// </summary>
    /// <typeparam name="T">资源类型。</typeparam>
    /// <param name="path">Resources 文件夹下的相对路径。</param>
    public T Load<T>(string path)
        where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }

    /// <summary>
    /// 使用泛型异步加载资源。
    /// </summary>
    public void LoadAsync<T>(
        string path,
        UnityAction<T> callBack)
        where T : UnityEngine.Object
    {
        MonoMgr.Instance.StartCoroutine(
            ReallyLoadAsync(path, callBack)
        );
    }

    /// <summary>
    /// 真正执行泛型异步加载的协程。
    /// </summary>
    private IEnumerator ReallyLoadAsync<T>(
        string path,
        UnityAction<T> callBack)
        where T : UnityEngine.Object
    {
        ResourceRequest request =
            Resources.LoadAsync<T>(path);

        yield return request;

        callBack?.Invoke(request.asset as T);
    }

    /// <summary>
    /// 根据 Type 异步加载资源。
    /// </summary>
    public void LoadAsync(
        string path,
        Type type,
        UnityAction<UnityEngine.Object> callBack)
    {
        MonoMgr.Instance.StartCoroutine(
            ReallyLoadAsync(path, type, callBack)
        );
    }

    /// <summary>
    /// 真正执行 Type 异步加载的协程。
    /// </summary>
    private IEnumerator ReallyLoadAsync(
        string path,
        Type type,
        UnityAction<UnityEngine.Object> callBack)
    {
        ResourceRequest request =
            Resources.LoadAsync(path, type);

        yield return request;

        callBack?.Invoke(request.asset);
    }

    /// <summary>
    /// 卸载指定的单个资源。
    /// 不能用于销毁场景中的 GameObject 实例。
    /// </summary>
    public void UnloadAsset(
        UnityEngine.Object assetToUnload)
    {
        if (assetToUnload == null)
            return;

        Resources.UnloadAsset(assetToUnload);
    }

    /// <summary>
    /// 异步卸载当前没有使用的资源。
    /// </summary>
    ///`Resources.UnloadAsset()` 主要用于卸载资源本身，不是拿来直接销毁场景里的 GameObject
    public void UnloadUnusedAssets(
        UnityAction callBack = null)
    {
        MonoMgr.Instance.StartCoroutine(
            ReallyUnloadUnusedAssets(callBack)
        );
    }

    /// <summary>
    /// 真正执行资源清理的协程。
    /// </summary>
    ///> 开始异步卸载当前没有被任何地方引用的资源。`UnloadUnusedAssets()` 不需要你指定某个资源。Unity 会自己检查哪些资源已经没人用了，然后回收它们。
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
2. Main.cs
```
using UnityEngine;

public class Main : MonoBehaviour
{
    private GameObject currentObj;

    private void Start()
    {
        // 泛型异步加载
        ResMgr.Instance.LoadAsync<GameObject>(
            "Test",
            prefab =>
            {
                if (prefab == null)
                {
                    Debug.LogError(
                        "Resources 中没有找到 Test"
                    );
                    return;
                }

                currentObj = Instantiate(prefab);
            }
        );

        // Type 异步加载
        ResMgr.Instance.LoadAsync(
            "Test",
            typeof(GameObject),
            asset =>
            {
                GameObject prefab = asset as GameObject;

                if (prefab == null)
                {
                    Debug.LogError(
                        "加载结果不是 GameObject"
                    );
                    return;
                }

                Instantiate(prefab);
            }
        );
    }

    private void Update()
    {
        // 按下空格测试同步加载
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameObject prefab =
                ResMgr.Instance.Load<GameObject>("Test");

            if (prefab != null)
            {
                Instantiate(prefab);
            }
        }

        // 按下 U 清理未使用资源
        if (Input.GetKeyDown(KeyCode.U))
        {
            ResMgr.Instance.UnloadUnusedAssets(() =>
            {
                Debug.Log("未使用资源清理完成");
            });
        }

        // 按下 D 销毁场景实例
        if (Input.GetKeyDown(KeyCode.D) &&
            currentObj != null)
        {
            Destroy(currentObj);
            currentObj = null;
        }
    }
}
```