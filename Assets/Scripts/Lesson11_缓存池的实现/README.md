# 一、对象池模块的实现思路
```
Dictionary<string, Stack<GameObject>>
```
保存不同类型的对象。
Dictionary：整个柜子
string：抽屉名字 / 对象类型标识
Stack\<GameObject>：某一类对象的缓存容器

# 二、为什么使用 Dictionary
`Dictionary` 用来根据对象类型快速找到对应的对象池。
string:作为对象类型的标识。
Stack\<GameObject>:保存这一类暂时未使用的对象。

# 三、为什么使用 Stack
`Stack` 是栈结构：后进先出LIFO
Push():将对象放入池中。
Pop():将对象从池中取出。
==对于对象池来说，一般不关心具体取出的是哪个缓存对象，所以使用 `Stack` 即可。==

# 四、从对象池获取对象
==先判断是否存在对应对象池，并判断池中是否还有对象。==
```
public GameObject GetObj(string name)
{
    GameObject obj;

    if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
    {
        obj = poolDic[name].Pop();
        obj.SetActive(true);
    }
    else
    {
        obj = Object.Instantiate(Resources.Load<GameObject>(name));
        obj.name = name;
    }
    return obj;
}
```

# 五、创建新对象
当池中没有可用对象时：
```
obj = Object.Instantiate(Resources.Load<GameObject>(name));
```
==通过 `Resources.Load` 根据名字加载 Prefab，然后实例化对象。
Unity 实例化以后对象名称通常会变成：Bullet(Clone)
所以重新设置名字：obj.name = name;
这样对象归还时，可以继续使用obj.name找到对应的池。==

# 六、对象回收
对象不用时，不再Destroy(obj);
而是PoolMgr.Instance.PushObj(obj);回收到对象池。
核心代码：
```
public void PushObj(GameObject obj)
{
    obj.SetActive(false);
    if (!poolDic.ContainsKey(obj.name))
    {
        poolDic.Add(obj.name,new Stack<GameObject>());
    }
    poolDic[obj.name].Push(obj);
}
```

# 七、对象名称的作用
**1.对象池使用obj.name作为对象类型的 Key。**
因此创建对象时：obj.name = name;
归还时：poolDic\[obj.name].Push(obj);
这样才能保证：
```
Bullet → Bullet池
Enemy → Enemy池
```

# 八、对象池对象的生命周期
对象第一次使用：
```
Resources.Load
      ↓
Instantiate
      ↓
SetActive(true)
```
使用结束：
```
PushObj
   ↓
SetActive(false)
   ↓
进入对象池
```
再次使用：
```
Pop
 ↓
SetActive(true)
 ↓
继续使用
```
因此对象通常经历：
```
创建一次
   ↓
使用
   ↓
回收
   ↓
复用
   ↓
回收
   ↓
复用……
```

# 九、清空对象池
```
public void ClearPool()
{
    poolDic.Clear();
}
```
作用：清空 Dictionary 中保存的对象引用