# 一、公共 Mono 模块
## 1. 为什么需要公共 Mono 模块
继承 `MonoBehaviour` 的类可以直接使用 Unity 提供的生命周期函数。
也可以启动和关闭协程：
```
StartCoroutine()
StopCoroutine()
```
通常用来处理：
- 每帧逻辑
- 物理帧逻辑
- 延迟执行
- 分步执行
- 异步流程
而普通 C# 类如果不继承 `MonoBehaviour`，默认无法直接使用这些能力。
==公共 Mono 模块的目的==：**提供一个统一的 MonoBehaviour 入口，让普通 C# 类也能间接使用 Unity 的帧更新和协程能力。**

# 二、公共 Mono 模块能解决什么问题
主要提供两类能力：
```
1. 统一帧更新
2. 统一协程调用
```
也就是让普通 C# 类能够：
- 注册 Update 逻辑
- 注册 FixedUpdate 逻辑
- 注册 LateUpdate 逻辑
- 开启协程
- 停止协程

# 三、统一管理帧更新
## 1. 普通 C# 类不能直接写 Update
```
public class EnemyManager
{
    void Update()
    {
    }
}
```
这里的 `Update()` 不会被 Unity 自动调用。
## 2. 使用事件统一转发
公共 Mono 管理器可以自己拥有：
```
Update()
```
然后通过事件把更新通知分发出去。
```
using System;
using UnityEngine;

public class MonoManager : MonoBehaviour
{
    public event Action OnUpdate;

    private void Update()
    {
        OnUpdate?.Invoke();
    }
}
```
普通 C# 类只需要注册：
```
MonoManager.Instance.OnUpdate += UpdateLogic;
```
这样就可以间接获得每帧调用能力。

# 四、为什么使用事件或委托
核心结构：
```
Unity
↓
MonoManager.Update()
↓
事件 / 委托
↓
普通 C# 类的更新逻辑
```
这样普通类不需要继承：
```
MonoBehaviour
```
也可以拥有类似 `Update()` 的更新效果。
