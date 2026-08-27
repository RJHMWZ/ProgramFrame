# 一、为什么使用枚举管理事件名
```csharp
public const string enemyDieEvent = "EnemyDieEvent";
```
字符串如果拼写错误，代码不会报错，但事件无法正常触发：
```csharp
EventMgr3.Instance.EventTrigger("EnemyDleEvent", this);
```
本节课把事件名统一放进枚举：
```csharp
public enum E_EventType4
{
    EnemyDie
}
```
使用时由编辑器自动提示，写错会直接产生编译错误：
```csharp
E_EventType4.EnemyDie
```

# 二、事件字典的变化
上一版使用字符串作为键：
```csharp
Dictionary<string, EventInfoBase3> eventDic;
```
本节课改为使用枚举：
```csharp
Dictionary<E_EventType4, EventInfoBase4> eventDic;
```
泛型事件结构不变，仍然使用：
```csharp
EventInfo4<T>
UnityAction<T>
```

# 三、事件的注册、触发和移除
注册事件：
```csharp
EventMgr4.Instance.AddEventListener<Enemy4>(
    E_EventType4.EnemyDie,
    UpGrade
);
```
触发事件：
```csharp
EventMgr4.Instance.EventTrigger(
    E_EventType4.EnemyDie,
    this
);
```
移除事件：
```csharp
EventMgr4.Instance.RemoveEventListener<Enemy4>(
    E_EventType4.EnemyDie,
    UpGrade
);
```
三处必须使用同一个枚举成员和同一种参数类型。

# 四、执行流程
```text
Enemy4 的 Hp 归零
        ↓
使用 E_EventType4.EnemyDie 触发事件
并传入当前 Enemy4
        ↓
EventMgr4 查找对应枚举键
        ↓
Level4 和 Player4 收到 Enemy4
        ↓
关卡通关、人物升级
```

# 五、完整代码
## 1. E_EventType4.cs
```csharp
/// <summary>
/// 统一管理所有事件类型。
/// </summary>
public enum E_EventType4
{
    /// <summary>
    /// 敌人死亡事件，参数类型：Enemy4。
    /// </summary>
    EnemyDie
}
```

## 2. EventMgr4.cs

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 所有事件信息的共同父类
public abstract class EventInfoBase4
{
}

// 保存带一个泛型参数的事件委托
public class EventInfo4<T> : EventInfoBase4
{
    public UnityAction<T> actions;

    public EventInfo4(UnityAction<T> action)
    {
        actions = action;
    }
}

// 保存无参数事件委托
public class EventInfo4 : EventInfoBase4
{
    public UnityAction actions;

    public EventInfo4(UnityAction action)
    {
        actions = action;
    }
}

public class EventMgr4 : Singleton2<EventMgr4>
{
    private EventMgr4()
    {
    }

    private readonly Dictionary<E_EventType4, EventInfoBase4> eventDic = new();

    // 触发带参数事件
    public void EventTrigger<T>(E_EventType4 eventType, T info)
    {
        if (eventDic.TryGetValue(eventType, out EventInfoBase4 eventInfo) &&
            eventInfo is EventInfo4<T> typedEventInfo)
        {
            typedEventInfo.actions?.Invoke(info);
        }
    }

    // 触发无参数事件
    public void EventTrigger(E_EventType4 eventType)
    {
        if (eventDic.TryGetValue(eventType, out EventInfoBase4 eventInfo) &&
            eventInfo is EventInfo4 noParameterEventInfo)
        {
            noParameterEventInfo.actions?.Invoke();
        }
    }

    // 添加带参数事件监听
    public void AddEventListener<T>(
        E_EventType4 eventType,
        UnityAction<T> unityAction)
    {
        if (unityAction == null)
        {
            Debug.LogError("输入的监听函数不合法");
            return;
        }

        if (eventDic.TryGetValue(eventType, out EventInfoBase4 eventInfo))
        {
            if (eventInfo is EventInfo4<T> typedEventInfo)
            {
                typedEventInfo.actions += unityAction;
            }
        }
        else
        {
            eventDic.Add(eventType, new EventInfo4<T>(unityAction));
        }
    }

    // 添加无参数事件监听
    public void AddEventListener(
        E_EventType4 eventType,
        UnityAction unityAction)
    {
        if (unityAction == null)
        {
            Debug.LogError("输入的监听函数不合法");
            return;
        }

        if (eventDic.TryGetValue(eventType, out EventInfoBase4 eventInfo))
        {
            if (eventInfo is EventInfo4 noParameterEventInfo)
            {
                noParameterEventInfo.actions += unityAction;
            }
        }
        else
        {
            eventDic.Add(eventType, new EventInfo4(unityAction));
        }
    }

    // 移除带参数事件监听
    public void RemoveEventListener<T>(
        E_EventType4 eventType,
        UnityAction<T> unityAction)
    {
        if (eventDic.TryGetValue(eventType, out EventInfoBase4 eventInfo) &&
            eventInfo is EventInfo4<T> typedEventInfo)
        {
            typedEventInfo.actions -= unityAction;

            if (typedEventInfo.actions == null)
            {
                eventDic.Remove(eventType);
            }
        }
    }

    // 移除无参数事件监听
    public void RemoveEventListener(
        E_EventType4 eventType,
        UnityAction unityAction)
    {
        if (eventDic.TryGetValue(eventType, out EventInfoBase4 eventInfo) &&
            eventInfo is EventInfo4 noParameterEventInfo)
        {
            noParameterEventInfo.actions -= unityAction;

            if (noParameterEventInfo.actions == null)
            {
                eventDic.Remove(eventType);
            }
        }
    }

    public void Clear()
    {
        eventDic.Clear();
    }

    public void Clear(E_EventType4 eventType)
    {
        eventDic.Remove(eventType);
    }
}
```

## 3. Enemy4.cs

```csharp
using UnityEngine;

public class Enemy4 : MonoBehaviour
{
    public string enemyName = "普通怪兽";
    public int enemyID = 1;
    public int Hp = 5;

    private bool isDead;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J) && !isDead)
        {
            Hp--;
            Debug.Log($"{enemyName}扣血，剩余生命值：{Hp}");

            if (Hp <= 0)
            {
                Die();
            }
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{enemyName}死亡");

        EventMgr4.Instance.EventTrigger(
            E_EventType4.EnemyDie,
            this
        );
    }
}
```

## 4. Level4.cs

```csharp
using UnityEngine;

public class Level4 : MonoBehaviour
{
    private void Awake()
    {
        EventMgr4.Instance.AddEventListener<Enemy4>(
            E_EventType4.EnemyDie,
            ClearLevel
        );
    }

    private void ClearLevel(Enemy4 enemy)
    {
        Debug.Log(
            $"击败敌人：{enemy.enemyName}，游戏成功通关"
        );
    }

    private void OnDestroy()
    {
        EventMgr4.Instance.RemoveEventListener<Enemy4>(
            E_EventType4.EnemyDie,
            ClearLevel
        );
    }
}
```

## 5. Player4.cs

```csharp
using UnityEngine;

public class Player4 : MonoBehaviour
{
    private void Awake()
    {
        EventMgr4.Instance.AddEventListener<Enemy4>(
            E_EventType4.EnemyDie,
            UpGrade
        );
    }

    private void UpGrade(Enemy4 enemy)
    {
        Debug.Log(
            $"击败编号为 {enemy.enemyID} 的敌人，人物升级"
        );
    }

    private void OnDestroy()
    {
        EventMgr4.Instance.RemoveEventListener<Enemy4>(
            E_EventType4.EnemyDie,
            UpGrade
        );
    }
}
```
