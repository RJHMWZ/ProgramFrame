using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventMgr : Singleton2<EventMgr>
{
    private EventMgr()
    {
        
    }

    private Dictionary<string,UnityAction> eventDic=new();

    public const string enemyDieEvent="EnemyDieEvent";

    /// <summary>
    /// 触发事件执行
    /// </summary>
    /// <param name="eventName">要监听的事件标识名</param>
    public void EventTrigger(string eventName)
    {
        if(eventDic.TryGetValue(eventName,out UnityAction unityAction))
        {
            unityAction?.Invoke();
        }
    }

    /// <summary>
    /// 外部添加事件监听
    /// </summary>
    /// <param name="eventName">要监听的事件标识名</param>
    /// <param name="unityAction">事件委托</param>
    public void AddEventListener(string eventName,UnityAction unityAction)
    {
        if(string.IsNullOrEmpty(eventName)||unityAction==null)
        {
            Debug.LogError("输入的事件名称或者函数不合法");
            return;
        }
        if (eventDic.ContainsKey(eventName))
        {
            eventDic[eventName]+=unityAction;
        }
        else
        {
            eventDic.Add(eventName,unityAction);
        }
    }

    /// <summary>
    /// 外部移除事件监听
    /// </summary>
    /// <param name="eventName">要监听的事件标识名</param>
    /// <param name="unityAction">事件委托</param>
    public void RemoveEventListener(string eventName,UnityAction unityAction)
    {
        if(string.IsNullOrEmpty(eventName)||unityAction==null)
        {
            Debug.LogError("输入的事件名称或者函数不合法");
            return;
        }
        if (eventDic.ContainsKey(eventName))
        {
            eventDic[eventName] -= unityAction;

            // 已经没有任何监听者时，删除该事件
            if (eventDic[eventName] == null)
            {
                eventDic.Remove(eventName);
            }
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
