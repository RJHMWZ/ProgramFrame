# 一、缓存池层级布局优化
## 1. 为什么要优化 Hierarchy 布局
对象池中的对象通常不会真正销毁，而是：obj.SetActive(false)，然后继续保留在场景中。当对象数量变多以后，Hierarchy 中可能出现大量失活对象。这样会导致：
- Hierarchy 非常混乱
- 不方便开发时查找对象
- 无法直观看出对象属于哪个对象池
- 调试对象池时不方便观察
因此可以把对象池中的对象进行分层管理，让对象与对应池之间的关系更加清晰。

# 二、对象池布局结构
可以把整个对象池组织成：
```
Pool
├── Bullet
│   ├── Bullet
│   ├── Bullet
│   └── Bullet
├── Enemy
│   ├── Enemy
│   └── Enemy
└── Effect
    ├── Effect
    └── Effect
```

# 三、PoolData
把每一种对象池的数据进一步封装为：PoolData
结构变成：Dictionary<string, PoolData>
`PoolData` 负责管理某一种对象池的数据和层级关系。

# 四、PoolData 的成员
```
public class PoolData
{
    private Stack<GameObject> dataStack = new();
    private GameObject rootObj;
    public int Count => dataStack.Count;
}
```
## dataStack
保存真正被回收的对象。
## rootObj
表示当前子池在 Hierarchy 中的根对象。
## Count
用于判断当前池中还有多少可复用对象。

# 五、创建子池根对象
```
public PoolData(GameObject root, string name)
{
    if (PoolMgr.isOpenLayout)
    {
        rootObj = new GameObject(name);
        rootObj.transform.SetParent(root.transform);
    }
}
```
这样每一种对象都有自己的 Hierarchy 节点。

# 六、对象回收时的层级处理
对象放回池中：
```
public void Push(GameObject obj)
{
    obj.SetActive(false);
    if (PoolMgr.isOpenLayout)
    {
        obj.transform.SetParent(rootObj.transform);
    }
    dataStack.Push(obj);
}
```

# 七、对象取出时的层级处理
从对象池取出：
```
public GameObject Pop()
{
    GameObject obj = dataStack.Pop();
    obj.SetActive(true);
    if (PoolMgr.isOpenLayout)
    {
        obj.transform.SetParent(null);
    }
    return obj;
}
```

# 八、Pool 根对象
`PoolMgr` 中增加：private GameObject poolObj作为整个对象池的根节点。
当第一次回收对象时：
```
if (poolObj == null && isOpenLayout)
{
    poolObj = new GameObject("Pool");
}
```
创建：Pool
之后所有子池：
```
Bullet
Enemy
Effect
```
都会放到这个对象下面。

# 九、PushObj 的变化
PushObj()不再自己负责：obj.SetActive(false)和obj.transform.SetParent(...)
而是把这些逻辑交给：PoolData.Push()。所以：
```
public void PushObj(GameObject obj)
{
    if (poolObj == null && isOpenLayout)
    {
        poolObj = new GameObject("Pool");
    }

    if (!poolDic.ContainsKey(obj.name))
    {
        poolDic.Add(obj.name,new PoolData(poolObj, obj.name));
    }
    poolDic[obj.name].Push(obj);
}
```
职责变成：
PoolMgr：找到对应 PoolData
PoolData：负责具体对象的保存和层级管理
这样比把所有逻辑都写在 `PoolMgr` 中更加清晰。

# 十、GetObj 的变化
获取对象时：
```
if (poolDic.ContainsKey(name) &&poolDic[name].Count > 0)
{
    obj = poolDic[name].Pop();
}
else
{
    obj = Object.Instantiate(Resources.Load<GameObject>(name));
    obj.name = name;
}
```
现在：PoolData.Pop()
内部已经负责：
- 从 Stack 取出
- 激活对象
- 解除父子关系
因此 `PoolMgr` 只负责判断：
	池里有：Pop()
	池里没有：Instantiate()

# 十一、是否开启布局功能
```
public static bool isOpenLayout = false;
```
用于控制是否启用 Hierarchy 布局。如果：isOpenLayout = true;则：
```
Pool
├── Bullet
├── Enemy
└── Effect
```
并且回收对象会建立父子关系。
如果：isOpenLayout = false;则只进行对象池的数据管理，不进行额外的 Transform 父子关系操作。

# 十二、为什么把布局功能做成可选
修改：Transform.SetParent()
本身也需要一定的处理。如果对象池中的对象大量、频繁地：
取出：SetParent(null)
回收：SetParent(root)
就会产生额外的层级调整。
因此布局功能主要方便：
```
开发
调试
观察对象池
```
如果不需要在 Hierarchy 中观察对象关系，可以关闭：
```
isOpenLayout = false;
```

# 十三、ClearPool
```
public void ClearPool()
{
    poolDic.Clear();
    poolObj = null;
}
```
因为下一次重新使用对象池时，需要重新建立池的布局结构。

# 十四、PoolData.cs
```
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
```

# 十五、PoolMgr.cs
```
using System.Collections;

using System.Collections.Generic;

using UnityEngine;

  

public class PoolMgr2 : Singleton2<PoolMgr2>

{

    private PoolMgr2()

    {

    }  

  

    private Dictionary<string,PoolData> poolDic=new ();

    private  GameObject poolRoot;

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
```