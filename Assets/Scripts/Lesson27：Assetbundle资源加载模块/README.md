# AssetBundle 异步加载：潜在问题分析

# 一、什么是真正的异步加载
AssetBundle 加载资源包含两个阶段：
```text
加载 AssetBundle 文件
        ↓
从 AssetBundle 中加载资源
```
如果只有第二步使用异步 API，第一步仍然同步，就不算完整的异步加载。
真正的异步加载需要：
- 异步加载 AssetBundle。
- 异步加载 AssetBundle 中的资源。

# 二、同步与异步加载冲突
异步加载 AssetBundle：
```csharp
AssetBundle.LoadFromFileAsync(bundlePath);
```
同步加载 AssetBundle：
```csharp
AssetBundle.LoadFromFile(bundlePath);
```
如果同一个 AB 包正在异步加载，又对它进行同步加载，会发生重复加载并报错。
```text
LoadFromFileAsync("test") 正在加载
                ↓
LoadFromFile("test") 再次加载
                ↓
同一个 AssetBundle 被重复加载
                ↓
			   报错
```

# 三、StopCoroutine 不能取消 AB 加载
```csharp
Coroutine coroutine = StartCoroutine(LoadBundleAsync());
StopCoroutine(coroutine);
```
`StopCoroutine()` 只停止协程后续代码，不会取消已经发起`AssetBundleCreateRequest`
```text
停止协程
    ↓
不再执行 yield return 后面的代码

但底层 AssetBundle 异步加载仍可能继续
```
因此停止协程后，不能立即认为该 AB 包已经停止加载，也不能马上同步加载同一个包。

# 四、完整代码
## 1. EditorResMgr.cs
```csharp
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 编辑器资源管理器。
/// Editor API 只在开发阶段生效。
/// </summary>
public class EditorResMgr : BaseManager<EditorResMgr>
{
    private const string RootPath = "Assets/Editor/ArtRes/";

    private EditorResMgr()
    {
    }

    /// <summary>
    /// 加载单个编辑器资源。
    /// </summary>
    public T LoadEditorRes<T>(string path)
        where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        string suffixName = GetSuffixName<T>();

        if (string.IsNullOrEmpty(suffixName))
        {
            Debug.LogError(
                $"没有配置资源类型 {typeof(T).Name} 的扩展名"
            );
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<T>(
            RootPath + path + suffixName
        );
#else
        return null;
#endif
    }

    /// <summary>
    /// 加载图集中的指定 Sprite。
    /// </summary>
    public Sprite LoadSprite(
        string path,
        string spriteName)
    {
#if UNITY_EDITOR
        UnityEngine.Object[] sprites =
            AssetDatabase.LoadAllAssetRepresentationsAtPath(
                RootPath + path
            );

        foreach (UnityEngine.Object item in sprites)
        {
            if (item is Sprite sprite &&
                sprite.name == spriteName)
            {
                return sprite;
            }
        }
#endif

        return null;
    }

    /// <summary>
    /// 加载图集中的所有 Sprite。
    /// </summary>
    public Dictionary<string, Sprite> LoadSprites(
        string path)
    {
        Dictionary<string, Sprite> spriteDic = new();

#if UNITY_EDITOR
        UnityEngine.Object[] sprites =
            AssetDatabase.LoadAllAssetRepresentationsAtPath(
                RootPath + path
            );

        foreach (UnityEngine.Object item in sprites)
        {
            if (item is Sprite sprite)
            {
                spriteDic[sprite.name] = sprite;
            }
        }
#endif

        return spriteDic;
    }

    private string GetSuffixName<T>()
        where T : UnityEngine.Object
    {
        System.Type type = typeof(T);

        if (type == typeof(GameObject))
            return ".prefab";

        if (type == typeof(Material))
            return ".mat";

        if (type == typeof(Texture) ||
            type == typeof(Texture2D) ||
            type == typeof(Sprite))
        {
            return ".png";
        }

        if (type == typeof(AudioClip))
            return ".mp3";

        return string.Empty;
    }
}
```

## 2. Main.cs
下面的代码用于复现同步与异步加载同一个 AB 包的冲突，不应直接用于正式项目。
```csharp
using System.Collections;
using System.IO;
using UnityEngine;

public class Main : MonoBehaviour
{
    private string bundlePath;

    private void Start()
    {
        bundlePath = Path.Combine(
            Application.streamingAssetsPath,
            "PC/test"
        );

        // 发起异步加载
        Coroutine coroutine = StartCoroutine(
            LoadBundleAsync()
        );

        // 只停止协程，不会取消底层 AssetBundleCreateRequest
        StopCoroutine(coroutine);

        // 此时同步加载同一个包可能产生重复加载错误
        AssetBundle bundle =
            AssetBundle.LoadFromFile(bundlePath);

        if (bundle != null)
        {
            Debug.Log($"同步加载完成：{bundle.name}");
        }
    }

    private IEnumerator LoadBundleAsync()
    {
        AssetBundleCreateRequest request =
            AssetBundle.LoadFromFileAsync(bundlePath);

        yield return request;

        if (request.assetBundle != null)
        {
            Debug.Log(
                $"异步加载完成：{request.assetBundle.name}"
            );
        }
    }
}
```
