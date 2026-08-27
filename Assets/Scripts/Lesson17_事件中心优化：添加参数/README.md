一、为什么要给事件添加参数
原来的事件只能通知监听者“敌人死亡了”，不能告诉监听者死亡的是哪个敌人。
添加参数后，`Enemy` 可以在触发死亡事件时把自身传给事件中心：
```csharp
EventMgr.Instance.EventTrigger
(
    EventMgr.enemyDieEvent,
    this
);
```
`Level` 和 `Player` 收到事件后，就能读取死亡敌人的名称、编号等信息。

# 二、事件字典的变化
原来的委托不接收参数：
```csharp
Dictionary<string, UnityAction> eventDic;
```
现在改为接收一个 `object` 参数：
```csharp
Dictionary<string, UnityAction<object>> eventDic;
```
`object` 是所有类型的父类，因此可以接收 `Enemy`、`GameObject`、数值或其他数据。

# 三、触发带参数的事件
```csharp
public void EventTrigger(string eventName, object info = null)
{
    if (eventDic.TryGetValue(
        eventName,
        out UnityAction<object> unityAction))
    {
        unityAction?.Invoke(info);
    }
}
```
- `eventName`：需要触发的事件名称。
- `info`：需要传给监听者的数据。
- `info = null`：不传参数时默认使用 `null`。

# 四、监听方法也要接收参数
添加参数后，注册到事件中心的方法必须接收一个 `object`：
```csharp
private void UpGrade(object info)
```
接收到数据后，将 `object` 转回原来的 `Enemy` 类型：
```csharp
Enemy enemy = info as Enemy;
```
转换成功后，就可以读取敌人数据：
```csharp
enemy.enemyName;
enemy.enemyID;
```

# 五、带参数事件的执行流程
```text
Enemy 的 Hp 归零
        ↓
Enemy 触发 enemyDieEvent
并把 this 作为参数传入
        ↓
EventMgr 执行 UnityAction<object>
        ↓
Level 和 Player 收到 object
        ↓
将 object 转换为 Enemy
        ↓
读取敌人信息并执行对应逻辑
```

# 六、object 传参存在的问题
`object` 可以接收不同类型的数据，使用比较方便。
但是传递 `int`、`float`、`bool` 等值类型时会发生装箱；取出并转换回值类型时会发生拆箱，会产生额外的性能开销。

# 七、完整代码
## 1. EventMgr.cs
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventMgr : Singleton2<EventMgr>
{
    private EventMgr()
    {
    }

    // Key：事件名称
    // Value：带有一个 object 参数的事件委托
    private readonly Dictionary<string, UnityAction<object>> eventDic = new();

    // 敌人死亡事件的唯一标识
    public const string enemyDieEvent = "EnemyDieEvent";

    /// <summary>
    /// 触发事件并传递参数。
    /// </summary>
    /// <param name="eventName">要触发的事件标识名。</param>
    /// <param name="info">需要传给监听者的数据。</param>
    public void EventTrigger(string eventName, object info = null)
    {
        if (eventDic.TryGetValue(
            eventName,
            out UnityAction<object> unityAction))
        {
            unityAction?.Invoke(info);
        }
    }

    /// <summary>
    /// 添加事件监听。
    /// </summary>
    /// <param name="eventName">要监听的事件标识名。</param>
    /// <param name="unityAction">接收 object 参数的事件委托。</param>
    public void AddEventListener(
        string eventName,
        UnityAction<object> unityAction)
    {
        if (string.IsNullOrEmpty(eventName) || unityAction == null)
        {
            Debug.LogError("输入的事件名称或者函数不合法");
            return;
        }

        if (eventDic.ContainsKey(eventName))
        {
            eventDic[eventName] += unityAction;
        }
        else
        {
            eventDic.Add(eventName, unityAction);
        }
    }

    /// <summary>
    /// 移除事件监听。
    /// </summary>
    /// <param name="eventName">要移除的事件标识名。</param>
    /// <param name="unityAction">需要移除的事件委托。</param>
    public void RemoveEventListener(
        string eventName,
        UnityAction<object> unityAction)
    {
        if (string.IsNullOrEmpty(eventName) || unityAction == null)
        {
            Debug.LogError("输入的事件名称或者函数不合法");
            return;
        }

        if (!eventDic.ContainsKey(eventName))
            return;

        eventDic[eventName] -= unityAction;

        // 已经没有监听者时，删除该事件记录
        if (eventDic[eventName] == null)
        {
            eventDic.Remove(eventName);
        }
    }

    /// <summary>
    /// 清空所有事件监听。
    /// </summary>
    public void Clear()
    {
        eventDic.Clear();
    }

    /// <summary>
    /// 清空指定事件的所有监听。
    /// </summary>
    public void Clear(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            eventDic.Remove(eventName);
        }
    }
}
```
## 2. Enemy.cs
```csharp
using UnityEngine;

public class Enemy : MonoBehaviour
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

        // 将当前死亡的 Enemy 作为参数传给事件中心
        EventMgr.Instance.EventTrigger(
            EventMgr.enemyDieEvent,
            this
        );
    }
}
```

## 3. Level.cs

```csharp
using UnityEngine;

public class Level : MonoBehaviour
{
    private void Awake()
    {
        EventMgr.Instance.AddEventListener(
            EventMgr.enemyDieEvent,
            ClearLevel
        );
    }

    // 带参数事件的监听方法必须接收一个 object
    private void ClearLevel(object info)
    {
        Enemy enemy = info as Enemy;

        if (enemy == null)
        {
            Debug.LogError("敌人死亡事件传入的数据不是 Enemy");
            return;
        }

        Debug.Log(
            $"击败敌人：{enemy.enemyName}，游戏成功通关"
        );
    }

    private void OnDestroy()
    {
        EventMgr.Instance.RemoveEventListener(
            EventMgr.enemyDieEvent,
            ClearLevel
        );
    }
}
```

## 4. Player.cs

```csharp
using UnityEngine;

public class Player : MonoBehaviour
{
    private void Awake()
    {
        EventMgr.Instance.AddEventListener(
            EventMgr.enemyDieEvent,
            UpGrade
        );
    }

    // 带参数事件的监听方法必须接收一个 object
    private void UpGrade(object info)
    {
        Enemy enemy = info as Enemy;

        if (enemy == null)
        {
            Debug.LogError("敌人死亡事件传入的数据不是 Enemy");
            return;
        }

        Debug.Log(
            $"击败编号为 {enemy.enemyID} 的敌人，人物升级"
        );
    }

    private void OnDestroy()
    {
        EventMgr.Instance.RemoveEventListener(
            EventMgr.enemyDieEvent,
            UpGrade
        );
    }
}
```
运行后连续按下 `J`，敌人生命值归零时会输出：
```text
普通怪兽死亡
击败敌人：普通怪兽，游戏成功通关
击败编号为 1 的敌人，人物升级
```
