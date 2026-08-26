using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoMgr : SingletonMonoAuto<MonoMgr>
{
    private event Action updateEvent;
    private event Action fixedUpdateEvent;
    private event Action lateUpdateEvent;

    #region 封装给外部的函数
    /// <summary>
    /// 添加外部类Update的函数委托
    /// </summary>
    /// <param name="action"></param>
    public void AddUpdateListener(Action action)
    {
        updateEvent+=action;
    }

    /// <summary>
    /// 移除外部类Update的函数委托
    /// </summary>
    /// <param name="action"></param>
    public void RemoveUpdateListener(Action action)
    {
        updateEvent-=action;
    }

    /// <summary>
    /// 添加外部类FixedUpdate的函数委托
    /// </summary>
    /// <param name="action"></param>
    public void AddFixedUpdateListener(Action action)
    {
        fixedUpdateEvent+=action;
    }

    /// <summary>
    /// 移除外部类Update的函数委托
    /// </summary>
    /// <param name="action"></param>
    public void RemoveFixedUpdateListener(Action action)
    {
        fixedUpdateEvent-=action;
    }

     /// <summary>
    /// 添加外部类LateUpdate的函数委托
    /// </summary>
    /// <param name="action"></param>
    public void AddLateUpdateListener(Action action)
    {
        lateUpdateEvent+=action;
    }

    /// <summary>
    /// 外部类LateUpdate的函数委托
    /// </summary>
    /// <param name="action"></param>
    public void RemoveLateUpdateListener(Action action)
    {
        lateUpdateEvent-=action;
    }

    /// <summary>
    /// 开启外部类的协程
    /// </summary>
    /// <param name="iEnumerator"> 协程要执行的内容/流程</param>
    public Coroutine StartCoroutineListener(IEnumerator iEnumerator)
    {
        return StartCoroutine(iEnumerator);
    }

    /// <summary>
    /// 结束外部类的协程
    /// </summary>
    /// <param name="coroutine">Unity 启动协程后返回的引用</param>
    public void StopCoroutineListener(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
    }

    #endregion

    #region 生命周期函数处理——真正执行委托事件函数逻辑
      void Update()
    {
        updateEvent?.Invoke();
    }

    void FixedUpdate()
    {
        fixedUpdateEvent?.Invoke();
    }

    void LateUpdate()
    {
        lateUpdateEvent?.Invoke();
    }
    #endregion
    
}
