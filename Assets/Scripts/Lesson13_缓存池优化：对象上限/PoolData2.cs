using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolData2
{
    private readonly GameObject objRoot;//抽屉——父物体
    public Stack<GameObject> objStack=new ();//当前没有使用的对象
    private List<GameObject> usedList = new List<GameObject>();//正在场景中被使用的对象
    public int Count=>objStack.Count;//当前没有使用的对象数量
    public int UsedCount => usedList.Count;//使用中对象数量

    /// <summary>
    /// 创建某一类对象对应的抽屉。
    /// 创建抽屉时产生的第一个对象正在使用，因此需要记录到 usedList 中。
    /// </summary>
    public PoolData2(GameObject poolRoot, string poolName,GameObject firstUsedObj)
    {
        if (PoolMgr3.Instance.IsOpenLayout)
        {
            objRoot = new GameObject(poolName);
            objRoot.transform.SetParent(poolRoot.transform);
        }
        PushUsedList(firstUsedObj);
    }
    
    /// <summary>
    /// 获取对象。
    /// 有空闲对象时取出空闲对象；没有空闲对象时复用最早使用的对象。
    /// </summary>
    public GameObject Pop()
    {
        GameObject obj;
        if (Count > 0)
        {
            // 优先取出空闲对象
            obj = objStack.Pop();
            usedList.Add(obj);
        }
        else
        {
            // 没有空闲对象，说明已达到上限
            // 取出使用时间最久的对象
            obj = usedList[0];
            usedList.RemoveAt(0);

            // 重新放到尾部，表示它刚刚开始新一轮使用
            usedList.Add(obj);
        }

        if (PoolMgr3.Instance.IsOpenLayout)
        {
            obj.transform.SetParent(null);
        }
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 归还对象。
    /// </summary>
    public void Push(GameObject obj)
    {
        if (obj == null)
            return;

        // 防止同一个对象被重复放入空闲栈
        if (objStack.Contains(obj))
            return;

        obj.SetActive(false);

        if (PoolMgr3.Instance.IsOpenLayout)
        {
            obj.transform.SetParent(objRoot.transform);
        }

        objStack.Push(obj);
        usedList.Remove(obj);
    }

    /// <summary>
    /// 记录一个新创建、正在使用的对象。
    /// </summary>
    public void PushUsedList(GameObject obj)
    {
        if (obj != null && !usedList.Contains(obj))
        {
            usedList.Add(obj);
        }
    }
}
