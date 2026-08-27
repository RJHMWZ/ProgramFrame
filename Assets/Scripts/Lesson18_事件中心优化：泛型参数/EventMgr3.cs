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