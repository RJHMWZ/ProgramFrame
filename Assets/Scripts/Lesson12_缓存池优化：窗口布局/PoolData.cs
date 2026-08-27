using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolData
{
    private readonly GameObject objRoot;//抽屉——父物体
    public Stack<GameObject> objStack=new ();//抽屉中所有保留的对象们
    public int Count=>objStack.Count;//抽屉中的对象数量

    public PoolData(GameObject poolRoot, string poolName)
    {
        if (PoolMgr2.Instance.IsOpenLayout)
        {
            objRoot = new GameObject(poolName);
            objRoot.transform.SetParent(poolRoot.transform);
        }
    }
    
    public GameObject Pop()
    {
        GameObject obj;
        obj=objStack.Pop();
        obj.SetActive(true);
        if (PoolMgr2.Instance.IsOpenLayout)
        {
            obj.transform.SetParent(null);
        }
        return obj;
    }

    public void Push(GameObject obj)
    {
        objStack.Push(obj);
        obj.SetActive(false);
        if (PoolMgr2.Instance.IsOpenLayout)
        {
            Debug.Log("保存到缓存池中");
            obj.transform.SetParent(objRoot.transform);
        }
    }
}
