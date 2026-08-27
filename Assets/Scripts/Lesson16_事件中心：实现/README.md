# 一、实现目标
敌人死亡时，需要通知其他系统执行对应逻辑：
```text
敌人死亡
├── Level：游戏成功通关
└── Player：人物升级
```
`Enemy` 不直接引用 `Level` 和 `Player`，而是通过 `EventMgr` 发送死亡事件，从而降低对象之间的耦合度。

# 二、事件中心的数据结构
```csharp
Dictionary<string, UnityAction> eventDic;
```
- `string`：事件的唯一标识名。
- `UnityAction`：事件触发时需要执行的方法。
- `Dictionary`：根据事件名称找到对应的所有监听方法。
敌人死亡事件使用常量保存：
```csharp
public const string EnemyDieEvent = "EnemyDieEvent";
```
使用常量可以统一事件名称，避免手写字符串时出现拼写错误。

# 三、添加事件监听
`Level` 和 `Player` 在 `Awake()` 中监听敌人死亡事件：
```csharp
EventMgr.Instance.AddEventListener
(
    EventMgr.EnemyDieEvent,
    ClearLevel
);
```
如果字典中已经存在该事件，就使用 `+=` 添加监听方法；否则创建新的事件记录。

# 四、触发事件
敌人血量归零后触发死亡事件：
```csharp
EventMgr.Instance.EventTrigger(EventMgr.EnemyDieEvent);
```
事件中心找到 `EnemyDieEvent` 对应的委托，并执行其中保存的全部方法。
```text
按下 J
  ↓
敌人扣除一点生命值
  ↓
Hp <= 0
  ↓
触发 EnemyDieEvent
  ↓
Level.ClearLevel()
Player.UpGrade()
```

# 五、防止重复死亡
```csharp
private bool isDead;
```
敌人死亡后把 `isDead` 设置为 `true`：
```csharp
isDead = true;
```
检测按键时判断敌人是否存活：
```csharp
if (Input.GetKeyDown(KeyCode.J) && !isDead)
```
这样可以防止生命值归零后重复触发死亡事件。

# 六、移除事件监听
监听者销毁时，需要移除自己注册的方法：
```csharp
private void OnDestroy()
{
    EventMgr.Instance.RemoveEventListener(
        EventMgr.EnemyDieEvent,
        ClearLevel
    );
}
```
```text
Awake     → 添加监听
OnDestroy → 移除监听
```
如果一个事件已经没有任何监听者，就从字典中删除该事件记录。

# 七、完整代码
## 1. EventMgr.cs
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventMgr : Singleton2<EventMgr>
{
    // Key：事件名称
    // Value：事件触发时需要执行的所有方法
    private readonly Dictionary<string, UnityAction> eventDic = new();

    // 敌人死亡事件的唯一标识
    public const string EnemyDieEvent = "EnemyDieEvent";

    private EventMgr()
    {
    }

    /// <summary>
    /// 触发事件。
    /// </summary>
    /// <param name="eventName">需要触发的事件名称。</param>
    public void EventTrigger(string eventName)
    {
        if (eventDic.TryGetValue(eventName, out UnityAction unityAction))
        {
            unityAction?.Invoke();
        }
    }

    /// <summary>
    /// 添加事件监听。
    /// </summary>
    /// <param name="eventName">需要监听的事件名称。</param>
    /// <param name="unityAction">事件触发时执行的方法。</param>
    public void AddEventListener(
        string eventName,
        UnityAction unityAction)
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
    /// 移除指定的事件监听。
    /// </summary>
    /// <param name="eventName">事件名称。</param>
    /// <param name="unityAction">需要移除的方法。</param>
    public void RemoveEventListener(
        string eventName,
        UnityAction unityAction)
    {
        if (string.IsNullOrEmpty(eventName) || unityAction == null)
        {
            Debug.LogError("输入的事件名称或者函数不合法");
            return;
        }

        if (!eventDic.ContainsKey(eventName))
            return;

        eventDic[eventName] -= unityAction;

        // 没有监听者后删除该事件
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
    public int Hp = 5;

    // 记录敌人是否已经死亡
    private bool isDead;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J) && !isDead)
        {
            Hp--;
            Debug.Log($"敌人扣血，剩余生命值：{Hp}");

            if (Hp <= 0)
            {
                Die();
            }
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("怪兽死亡");

        EventMgr.Instance.EventTrigger(
            EventMgr.EnemyDieEvent
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
            EventMgr.EnemyDieEvent,
            ClearLevel
        );
    }

    private void ClearLevel()
    {
        Debug.Log("游戏成功通关");
    }

    private void OnDestroy()
    {
        EventMgr.Instance.RemoveEventListener(
            EventMgr.EnemyDieEvent,
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
            EventMgr.EnemyDieEvent,
            UpGrade
        );
    }

    private void UpGrade()
    {
        Debug.Log("人物升级");
    }

    private void OnDestroy()
    {
        EventMgr.Instance.RemoveEventListener(
            EventMgr.EnemyDieEvent,
            UpGrade
        );
    }
}
```
运行游戏后连续按下 `J`：
```text
敌人扣血，剩余生命值：4
敌人扣血，剩余生命值：3
敌人扣血，剩余生命值：2
敌人扣血，剩余生命值：1
敌人扣血，剩余生命值：0
怪兽死亡
游戏成功通关
人物升级
```
