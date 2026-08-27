using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolMgr2 : Singleton2<PoolMgr2>
{
    private PoolMgr2()
    {
        
    }   

    private Dictionary<string,PoolData> poolDic=new ();
    private  GameObject poolRoot;
    public bool IsOpenLayout=true;
    public GameObject GetObj(string name)
    {
        GameObject obj;
        //存在抽屉并且有对象
        if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
        {
            obj=poolDic[name].Pop();
        }
        else
        {
            obj=GameObject.Instantiate(Resources.Load<GameObject>(name));
            obj.name=name;
        }
        return obj;
    }

    public void PushObj(GameObject obj)
    {
       if (obj == null)
            return;
        // 第一次需要布局时创建总根对象
        if (IsOpenLayout && poolRoot == null)
        {
            poolRoot = new GameObject("Pool");
        }

        // 没有对应的子池就创建
        if (!poolDic.TryGetValue(obj.name, out PoolData poolData))
        {
            poolData = new PoolData(poolRoot, obj.name);
            poolDic.Add(obj.name, poolData);
        }
        poolData.Push(obj);
    }

    public void ClearPool()
    {
        poolDic.Clear();
        poolRoot = null;
    }
}
