# 一、为什么改用泛型事件
使用 `object` 传递事件参数，监听者收到参数后需要手动转换：
```csharp
Enemy2 enemy = info as Enemy2;
```
本节课使用泛型指定参数类型：
```csharp
EventMgr3.Instance.AddEventListener<Enemy3>(
    EventMgr3.enemyDieEvent,
    UpGrade
);
```
监听方法可以直接接收 `Enemy3`：
```csharp
private void UpGrade(Enemy3 enemy)
```
这样不需要类型转换，传递值类型时也能避免 `object` 产生的装箱和拆箱。

# 二、事件字典的变化
不同事件可能传递不同类型，因此先定义共同父类：
```csharp
public abstract class EventInfoBase3
{
}
```
带参数事件使用 `EventInfo3<T>` 保存：
```csharp
public class EventInfo3<T> : EventInfoBase3
{
    public UnityAction<T> actions;
}
```
无参数事件使用 `EventInfo3` 保存：
```csharp
public class EventInfo3 : EventInfoBase3
{
    public UnityAction actions;
}
```
字典统一保存它们的父类：
```csharp
Dictionary<string, EventInfoBase3> eventDic;
```

# 三、泛型事件的使用
注册带 `Enemy3` 参数的事件：
```csharp
EventMgr3.Instance.AddEventListener<Enemy3>(
    EventMgr3.enemyDieEvent,
    ClearLevel
);
```
触发事件并传入当前敌人：
```csharp
EventMgr3.Instance.EventTrigger(
    EventMgr3.enemyDieEvent,
    this
);
```
`this` 是 `Enemy3`，所以编译器会自动推断泛型参数为 `Enemy3`。
移除监听时必须使用相同类型：
```csharp
EventMgr3.Instance.RemoveEventListener<Enemy3>(
    EventMgr3.enemyDieEvent,
    ClearLevel
);
```

# 四、无参数事件
事件中心通过方法重载同时支持无参数事件：
```csharp
EventMgr3.Instance.AddEventListener("Test", Test);
EventMgr3.Instance.EventTrigger("Test");
EventMgr3.Instance.RemoveEventListener("Test", Test);
```
无参数监听方法不需要形参：
```csharp
private void Test()
{
    Debug.Log("无参数事件被触发");
}
```

# 五、执行流程
```text
Enemy3 的 Hp 归零
        ↓
触发 enemyDieEvent，并传入 this
        ↓
EventMgr3 执行 UnityAction<Enemy3>
        ↓
Level3.ClearLevel(Enemy3 enemy)
Player3.UpGrade(Enemy3 enemy)
        ↓
读取死亡敌人的名称和编号
```

# 六、完整代码
## 1. EventMgr3.cs
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 所有事件信息的共同父类
public abstract class EventInfoBase3
{
}

// 保存带一个泛型参数的事件委托
public class EventInfo3<T> : EventInfoBase3
{
    public UnityAction<T> actions;

    public EventInfo3(UnityAction<T> action)
    {
        actions = action;
    }
}

// 保存无参数事件委托
public class EventInfo3 : EventInfoBase3
{
    public UnityAction actions;

    public EventInfo3(UnityAction action)
    {
        actions = action;
    }
}

public class EventMgr3 : Singleton2<EventMgr3>
{
    private EventMgr3()
    {
    }

    private readonly Dictionary<string, EventInfoBase3> eventDic = new();

    public const string enemyDieEvent = "EnemyDieEvent";

    // 触发带参数事件
    public void EventTrigger<T>(string eventName, T info)
    {
        if (eventDic.TryGetValue(eventName, out EventInfoBase3 eventInfo) &&
            eventInfo is EventInfo3<T> typedEventInfo)
        {
            typedEventInfo.actions?.Invoke(info);
        }
    }

    // 触发无参数事件
    public void EventTrigger(string eventName)
    {
        if (eventDic.TryGetValue(eventName, out EventInfoBase3 eventInfo) &&
            eventInfo is EventInfo3 noParameterEventInfo)
        {
            noParameterEventInfo.actions?.Invoke();
        }
    }

    // 添加带参数事件监听
    public void AddEventListener<T>(
        string eventName,
        UnityAction<T> unityAction)
    {
        if (string.IsNullOrEmpty(eventName) || unityAction == null)
        {
            Debug.LogError("输入的事件名称或者函数不合法");
            return;
        }

        if (eventDic.TryGetValue(eventName, out EventInfoBase3 eventInfo))
        {
            if (eventInfo is EventInfo3<T> typedEventInfo)
            {
                typedEventInfo.actions += unityAction;
            }
        }
        else
        {
            eventDic.Add(eventName, new EventInfo3<T>(unityAction));
        }
    }

    // 添加无参数事件监听
    public void AddEventListener(
        string eventName,
        UnityAction unityAction)
    {
        if (string.IsNullOrEmpty(eventName) || unityAction == null)
        {
            Debug.LogError("输入的事件名称或者函数不合法");
            return;
        }

        if (eventDic.TryGetValue(eventName, out EventInfoBase3 eventInfo))
        {
            if (eventInfo is EventInfo3 noParameterEventInfo)
            {
                noParameterEventInfo.actions += unityAction;
            }
        }
        else
        {
            eventDic.Add(eventName, new EventInfo3(unityAction));
        }
    }

    // 移除带参数事件监听
    public void RemoveEventListener<T>(
        string eventName,
        UnityAction<T> unityAction)
    {
        if (eventDic.TryGetValue(eventName, out EventInfoBase3 eventInfo) &&
            eventInfo is EventInfo3<T> typedEventInfo)
        {
            typedEventInfo.actions -= unityAction;

            if (typedEventInfo.actions == null)
            {
                eventDic.Remove(eventName);
            }
        }
    }

    // 移除无参数事件监听
    public void RemoveEventListener(
        string eventName,
        UnityAction unityAction)
    {
        if (eventDic.TryGetValue(eventName, out EventInfoBase3 eventInfo) &&
            eventInfo is EventInfo3 noParameterEventInfo)
        {
            noParameterEventInfo.actions -= unityAction;

            if (noParameterEventInfo.actions == null)
            {
                eventDic.Remove(eventName);
            }
        }
    }

    public void Clear()
    {
        eventDic.Clear();
    }

    public void Clear(string eventName)
    {
        eventDic.Remove(eventName);
    }
}
```

## 2. Enemy3.cs

```csharp
using UnityEngine;

public class Enemy3 : MonoBehaviour
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

        EventMgr3.Instance.EventTrigger(
            EventMgr3.enemyDieEvent,
            this
        );
    }
}
```

## 3. Level3.cs

```csharp
using UnityEngine;

public class Level3 : MonoBehaviour
{
    private void Awake()
    {
        EventMgr3.Instance.AddEventListener<Enemy3>(
            EventMgr3.enemyDieEvent,
            ClearLevel
        );
    }

    private void ClearLevel(Enemy3 enemy)
    {
        Debug.Log(
            $"击败敌人：{enemy.enemyName}，游戏成功通关"
        );
    }

    private void OnDestroy()
    {
        EventMgr3.Instance.RemoveEventListener<Enemy3>(
            EventMgr3.enemyDieEvent,
            ClearLevel
        );
    }
}
```

## 4. Player3.cs

```csharp
using UnityEngine;

public class Player3 : MonoBehaviour
{
    private void Awake()
    {
        EventMgr3.Instance.AddEventListener<Enemy3>(
            EventMgr3.enemyDieEvent,
            UpGrade
        );
    }

    private void UpGrade(Enemy3 enemy)
    {
        Debug.Log(
            $"击败编号为 {enemy.enemyID} 的敌人，人物升级"
        );
    }

    private void OnDestroy()
    {
        EventMgr3.Instance.RemoveEventListener<Enemy3>(
            EventMgr3.enemyDieEvent,
            UpGrade
        );
    }
}
```
