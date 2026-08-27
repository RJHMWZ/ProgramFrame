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