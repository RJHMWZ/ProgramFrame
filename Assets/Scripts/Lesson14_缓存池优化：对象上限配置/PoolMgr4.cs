using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolMgr4 : Singleton2<PoolMgr4>
{
    private PoolMgr4()
    {
        
    }   

    private readonly Dictionary<string, PoolData3> poolDic = new();   // 柜子：Key 是对象类型，Value 是对应的抽屉
    private GameObject poolRoot;// 对象池在 Hierarchy 中的总根物体
    private int maxNum;
    public bool IsOpenLayout = true;// 是否开启 Hierarchy 层级整理

    /// <summary>
    /// 从对象池获取对象。
    /// </summary>
    /// <param name="name">Resources 文件夹下的资源路径。</param>
    public GameObject GetObj(string name)
    {
       if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("对象池资源路径不能为空");
            return null;
        }
        // 情况一：还没有这种对象对应的抽屉
        if (!poolDic.TryGetValue(name, out PoolData3 PoolData3))
        {
            GameObject newObj = CreateObj(name);
            if (newObj == null)
                return null;
            CreatePoolRoot();

            PoolData3 = new PoolData3(poolRoot, name, newObj);
            poolDic.Add(name, PoolData3);
            return newObj;
        }

        // 情况二：抽屉中存在空闲对象，直接复用
        if (PoolData3.Count > 0)
        {
            return PoolData3.Pop();
        }

        // 情况三：没有空闲对象，但还没有达到数量上限
        if (PoolData3.UsedCount < maxNum)
        {
            GameObject newObj = CreateObj(name);

            if (newObj == null)
                return null;

            PoolData3.PushUsedList(newObj);
            return newObj;
        }

        // 情况四：没有空闲对象，并且已经达到数量上限
        // Pop 会取出 usedList[0]，即使用时间最久的对象
        return PoolData3.Pop();
    }

    /// <summary>
    /// 将对象归还到对应的对象池。
    /// </summary>
    public void PushObj(GameObject obj)
    {
       if (obj == null)
            return;

        CreatePoolRoot();

        // 正常情况下，对象对应的抽屉已经在 GetObj 时创建
        // 此判断也可以兼容不是通过 GetObj 创建、但需要放入对象池的对象
        if (!poolDic.TryGetValue(obj.name, out PoolData3 PoolData3))
        {
            PoolData3 = new PoolData3(poolRoot, obj.name, obj);
            poolDic.Add(obj.name, PoolData3);
        }

        PoolData3.Push(obj);
    }

    /// <summary>
    /// 根据资源路径加载并实例化对象。
    /// </summary>
    private GameObject CreateObj(string name)
    {
        GameObject prefab = Resources.Load<GameObject>(name);
        if (prefab == null)
        {
            Debug.LogError($"Resources 中没有找到对象：{name}");
            return null;
        }
        GameObject obj = Object.Instantiate(prefab);
        if (obj.GetComponent<PoolObj>()!=null)
        {
            maxNum=obj.GetComponent<PoolObj>().MaxNum;
        }
        else
        {
            Debug.LogError("缓存池物体没有挂载PoolObj");
            return null;
        }
        obj.name = name;
        return obj;
    }

    /// <summary>
    /// 开启布局管理时，确保 Pool 根物体存在。
    /// </summary>
    private void CreatePoolRoot()
    {
        if (IsOpenLayout && poolRoot == null)
        {
            poolRoot = new GameObject("Pool");
        }
    }

    /// <summary>
    /// 清空对象池记录，通常在切换场景时调用。
    /// </summary>
    public void ClearPool()
    {
        poolDic.Clear();

        if (poolRoot != null)
        {
            Object.Destroy(poolRoot);
            poolRoot = null;
        }
    }
}
