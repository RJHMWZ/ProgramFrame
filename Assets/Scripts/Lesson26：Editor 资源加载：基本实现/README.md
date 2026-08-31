# 一、使用的核心 API
## 1. 加载单个资源
```csharp
AssetDatabase.LoadAssetAtPath<T>(path);
```
根据完整工程路径加载一个指定类型的资源。
## 2. 加载资源中的所有子资源
```csharp
AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
```
用于获取图集或多 Sprite 图片中的所有子资源。
两个 API 的路径都从 `Assets/` 开始，==并且需要包含文件扩展名==。

# 二、资源根目录
```csharp
private const string RootPath = "Assets/Editor/ArtRes/";
```
准备在开发阶段直接加载、以后再打入 AssetBundle 的资源统一放在该目录中。
外部调用时只需要传入相对于 `RootPath` 的路径：
```text
完整路径：Assets/Editor/ArtRes/Prefabs/Player.prefab
传入路径：Prefabs/Player
```

# 三、加载单个资源
```csharp
public T LoadEditorRes<T>(string path)where T : UnityEngine.Object
```
方法根据资源类型补充扩展名：

| 资源类型 | 扩展名 |
|---|---|
| `GameObject` | `.prefab` |
| `Material` | `.mat` |
| `Texture` / `Texture2D` / `Sprite` | `.png` |
| `AudioClip` | `.mp3` |
然后组成完整路径并加载：
```csharp
return AssetDatabase.LoadAssetAtPath<T>
(
    RootPath + path + suffixName
);
```
调用示例：
```csharp
GameObject prefab =
    EditorResMgr.Instance.LoadEditorRes<GameObject>(
        "Prefabs/Player"
    );
```

# 四、加载图集中的指定 Sprite
```csharp
public Sprite LoadSprite(string path,string spriteName)
```
执行过程：
```text
加载图片中的所有子资源
        ↓
遍历子资源
        ↓
找到名称等于 spriteName 的 Sprite
        ↓
返回该 Sprite
```
调用图集方法时，`path` 需要包含文件扩展名：
```csharp
Sprite icon = EditorResMgr.Instance.LoadSprite(
    "UI/Icons.png",
    "Sword"
);
```

# 五、加载图集中的所有 Sprite
```csharp
public Dictionary<string, Sprite> LoadSprites(string path)
```
图集中的 Sprite 以名称作为 Key 保存：
```text
Sword  → Sword Sprite
Shield → Shield Sprite
Coin   → Coin Sprite
```
外部可以根据名称快速获取：
```csharp
Dictionary<string, Sprite> sprites =
    EditorResMgr.Instance.LoadSprites("UI/Icons.png");

Sprite sword = sprites["Sword"];
```

# 六、路径规则

| 方法 | path 是否包含扩展名 |
|---|---|
| `LoadEditorRes<T>()` | 不包含，由管理器自动补充 |
| `LoadSprite()` | 包含，例如 `UI/Icons.png` |
| `LoadSprites()` | 包含，例如 `UI/Icons.png` |

# 七、使用限制
1. `AssetDatabase` 属于 `UnityEditor`，只能在 Unity 编辑器中使用。
2. `EditorResMgr.cs` 应放在 `Editor` 文件夹中，不能参与正式版本编译。
3. 当前后缀映射只支持代码中列出的格式，其他格式需要继续补充。
4. Editor 加载只用于开发阶段；测试或发布时应切换为 AssetBundle 等运行时加载方式。

# 八、完整代码
## EditorResMgr.cs
```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器资源管理器。
/// 只能在 Unity 编辑器中使用。
/// </summary>
public class EditorResMgr : BaseManager<EditorResMgr>
{
    // 编辑器资源的统一根目录
    private const string RootPath = "Assets/Editor/ArtRes/";

    private EditorResMgr()
    {
    }

    /// <summary>
    /// 加载单个编辑器资源。
    /// 传入路径不需要包含扩展名。
    /// </summary>
    public T LoadEditorRes<T>(string path)
        where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("编辑器资源路径不能为空");
            return null;
        }

        string suffixName = GetSuffixName<T>();

        if (string.IsNullOrEmpty(suffixName))
        {
            Debug.LogError(
                $"尚未配置资源类型 {typeof(T).Name} 的扩展名"
            );
            return null;
        }

        string fullPath = RootPath + path + suffixName;

        T resource =
            AssetDatabase.LoadAssetAtPath<T>(fullPath);

        if (resource == null)
        {
            Debug.LogError($"没有找到编辑器资源：{fullPath}");
        }

        return resource;
    }

    /// <summary>
    /// 加载图集中的指定 Sprite。
    /// 传入路径需要包含扩展名。
    /// </summary>
    public Sprite LoadSprite(
        string path,
        string spriteName)
    {
        if (string.IsNullOrEmpty(path) ||
            string.IsNullOrEmpty(spriteName))
        {
            Debug.LogError("图集路径或者 Sprite 名称不能为空");
            return null;
        }

        string fullPath = RootPath + path;

        UnityEngine.Object[] assets =
            AssetDatabase.LoadAllAssetRepresentationsAtPath(
                fullPath
            );

        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Sprite sprite &&
                sprite.name == spriteName)
            {
                return sprite;
            }
        }

        Debug.LogError(
            $"图集 {fullPath} 中没有找到 Sprite：{spriteName}"
        );

        return null;
    }

    /// <summary>
    /// 加载图集中的所有 Sprite。
    /// 传入路径需要包含扩展名。
    /// </summary>
    public Dictionary<string, Sprite> LoadSprites(
        string path)
    {
        Dictionary<string, Sprite> spriteDic = new();

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("图集路径不能为空");
            return spriteDic;
        }

        string fullPath = RootPath + path;

        UnityEngine.Object[] assets =
            AssetDatabase.LoadAllAssetRepresentationsAtPath(
                fullPath
            );

        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                spriteDic[sprite.name] = sprite;
            }
        }

        return spriteDic;
    }

    /// <summary>
    /// 根据资源类型获取文件扩展名。
    /// </summary>
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
