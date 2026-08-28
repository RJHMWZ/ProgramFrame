# 一、常见的资源加载方式
Unity 项目中可能同时使用多种资源加载方式：
- `Resources`：加载 `Resources` 文件夹中的资源。
- `AssetBundle`：加载本地或远程资源包。
- `UnityWebRequest `：从本地路径或网络获取资源和数据。
- `AssetDatabase`：在 Unity 编辑器中加载和管理工程资源。
- `Addressables`：通过地址统一加载本地或远程资源。
- `System.IO`：读写文件数据。
资源还可能位于：
- `Resources` 文件夹
- `StreamingAssets` 路径
- `persistentDataPath` 路径
- AssetBundle 或远程服务器

# 二、为什么需要资源加载模块
实际开发中可能组合使用多种加载方式。如果加载代码分散在不同系统中，会出现两个主要问题：
1. 各种资源的加载和卸载逻辑分散，不方便统一管理。
2. 异步加载经常需要协程或回调，容易产生大量重复代码。

# 三、资源加载模块的主要作用
资源加载模块主要负责：
- 统一管理不同资源的加载方式。
- 统一管理资源卸载。
- 封装同步和异步加载流程。
- 让外部根据需求选择合适的加载模块。
外部系统只负责提出“加载什么资源”，具体怎样加载由对应的资源管理器处理。

# 四、资源加载模块的基本原理
先将不同加载方式分别模块化：
```text
ResourcesMgr       → 管理 Resources 资源
AssetBundleMgr     → 管理 AssetBundle 资源
UnityWebRequestMgr → 管理本地或网络资源
EditorResourceMgr  → 管理编辑器资源
Addressables       → 管理可寻址资源
```
然后通过上层资源加载模块进行整合：
```text
             业务系统
                 ↓
          资源加载统一入口
                 ↓
    ┌────────────┼────────────┐
    ↓            ↓            ↓
ResourcesMgr  AssetBundleMgr  其他加载模块
```
调用者根据项目需求选择具体加载方式，各加载模块只负责自己对应的资源。

# 五、当前 Unity 中的选择
`Resources` 仍然可以用于简单项目和快速原型，但大量使用可能增加构建体积、内存占用和启动时间。当前 Unity 的 `Addressables` 可以通过地址异步加载本地或远程资源，并自动处理资源依赖。对于需要统一资源组织和异步加载的新项目，通常优先考虑 `Addressables`。本节只需要理解各种加载方式应被分别封装并统一管理，暂不涉及具体代码实现。