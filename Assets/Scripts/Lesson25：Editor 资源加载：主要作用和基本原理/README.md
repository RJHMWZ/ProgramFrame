# 一、为什么需要 Editor 资源加载
正式发布的项目通常会使用 `AssetBundle` 管理大部分游戏资源，以减小包体并支持热更新。
但是在开发阶段频繁打包 AssetBundle 会降低开发效率，因此可以直接从 Unity 工程中加载资源，等到最终测试或发布时再改用 AssetBundle。

# 二、Editor 资源加载的主要作用
- 避免开发阶段频繁打 AssetBundle。
- 提高功能开发和资源调试效率。
- 方便管理工程资源。
- 避免同一资源同时进入 Resources 和 AssetBundle，造成重复打包。

# 三、开发与发布阶段的加载方式
```text
项目开发阶段
    ↓
Editor 资源加载
直接读取 Unity 工程中的资源

最终测试或发布阶段
    ↓
AssetBundle 资源加载
读取 StreamingAssets 或远程服务器中的资源包
```
`Resources` 通常只保存少量默认必备资源，不用于存放准备由 AssetBundle 管理的大量资源。

# 四、Editor 资源加载的基本原理
Editor 资源加载依靠 `UnityEditor` 提供的编辑器扩展 API。
主要有两种方式：==AssetDatabase.LoadAssetAtPath==和==EditorGUIUtility.Load==

# 五、AssetDatabase.LoadAssetAtPath
用于根据工程路径加载资源：
```csharp
AssetDatabase.LoadAssetAtPath<T>(path);
```
路径从 `Assets` 开始，并包含文件扩展名：
```csharp
"Assets/GameResources/Prefabs/Player.prefab"
```
适合加载工程中指定位置的资源。

# 六、EditorGUIUtility.Load
用于加载 `Editor Default Resources` 文件夹中的资源：
```csharp
EditorGUIUtility.Load(path);
```
加载时填写相对于 `Editor Default Resources` 文件夹的路径。
该方式返回：
```csharp
UnityEngine.Object
```
使用前通常需要转换成具体资源类型。

# 七、两种方式的区别

| API | 加载范围 | 路径特点 |
|---|---|---|
| `AssetDatabase.LoadAssetAtPath<T>()` | Unity 工程中的指定资源 | 从 `Assets/` 开始，包含扩展名 |
| `EditorGUIUtility.Load()` | `Editor Default Resources` 中的资源 | 使用相对路径 |

# 八、使用限制
这两个 API 都属于 `UnityEditor`，只能在 Unity 编辑器中使用，不能在发布后的游戏中运行。相关代码应放入 `Editor` 文件夹，或者使用条件编译：
```csharp
#if UNITY_EDITOR
using UnityEditor;
#endif
```
发布版本必须切换为 AssetBundle 或其他运行时资源加载方式。
