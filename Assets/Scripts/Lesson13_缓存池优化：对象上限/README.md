# 一、为什么要限制对象池的数量
原对象池在空闲池中没有对象时，会不断实例化新对象。
如果已经取出的对象长时间不归还，每次调用 `GetObj()` 都可能创建新对象，导致同类对象的数量没有上限。
- 占用更多内存。
- 增加场景中的对象数量。
- 增加脚本更新、物理计算和渲染压力。
- 大量实例化还可能引起卡顿。

# 二、数量上限优化的核心思想
为每一类对象设置最大数量 `maxNum`。
有空闲对象 → 复用空闲对象
无空闲对象 + 未达上限 → 创建新对象
无空闲对象 + 已达上限 → 抢占最早使用的对象

# 三、对象池的数据结构
管理器使用字典保存不同类型的对象池：
```csharp
Dictionary<string, PoolData> poolDic;
```
- `Dictionary`：整个柜子。
- `string`：抽屉名称（键），也是对象类型的标识。
- `PoolData`：某一类对象对应的抽屉。
每个 `PoolData` 内部包含两个容器：
```csharp
private Stack<GameObject> dataStack = new Stack<GameObject>();
private List<GameObject> usedList = new List<GameObject>();
```
- `dataStack`：保存当前没有使用的对象。==空闲对象==
- `usedList`：按开始使用的先后顺序记录正在使用的对象。==使用中的对象==

# 四、为什么同时使用 Stack 和 List
## 1. Stack：管理空闲对象
==对象池通常不关心具体取出哪个空闲对象==，所以直接取出栈顶对象即可。
```csharp
dataStack.Push(obj); // 归还对象
dataStack.Pop();     // 取出对象
```
## 2. List：记录使用顺序
==新投入使用的对象添加到列表尾部，符合新投入使用的，放到最后才会被复用==
```csharp
usedList.Add(obj);
```
- `usedList[0]`：使用时间最久的对象。
- 列表尾部：最近开始使用的对象。
==达到数量上限时，可以通过 `usedList[0]` 找到最早使用的对象并进行复用。==

# 五、对象数量的记录
```csharp
public int Count => dataStack.Count;
public int UsedCount => usedList.Count;
```
- `Count`：当前空闲对象数量。
- `UsedCount`：当前正在使用的对象数量。
这两个属性是只读属性，外部只能读取数量，不能直接修改内部容器。

# 六、从对象池获取对象
```csharp
public GameObject GetObj(string name, int maxNum = 50)
{
    GameObject obj;

    if (!poolDic.ContainsKey(name) ||
        (poolDic[name].Count == 0 && poolDic[name].UsedCount < maxNum))
    {
        obj = GameObject.Instantiate(Resources.Load<GameObject>(name));
        obj.name = name;

        if (!poolDic.ContainsKey(name))
            poolDic.Add(name, new PoolData(poolObj, name, obj));
        else
            poolDic[name].PushUsedList(obj);
    }
    else
    {
        obj = poolDic[name].Pop();
    }

    return obj;
}
```
判断条件可以拆成三种情况：

| 情况 | 处理方式 |
|---|---|
| 不存在对应对象池 | 创建对象，并创建对应的 `PoolData` |
| 没有空闲对象，且使用数量小于上限 | 创建新对象，并加入 `usedList` |
| 存在空闲对象 | 从 `dataStack` 中取出 |
| 没有空闲对象，且已经达到上限 | 复用 `usedList[0]` |
`maxNum` 的默认值为 `50`：
```csharp
public GameObject GetObj(string name, int maxNum = 50)
```
调用者不传参数时，单类对象默认最多创建 50 个。

# 七、创建新对象
```csharp
obj = GameObject.Instantiate(Resources.Load<GameObject>(name));
obj.name = name;
```

# 八、PoolData.Pop() 的两种复用方式
```csharp
public GameObject Pop()
{
    GameObject obj;
    if (Count > 0)
    {
        obj = dataStack.Pop();
        usedList.Add(obj);
    }
    else
    {
        obj = usedList[0];
        usedList.RemoveAt(0);
        usedList.Add(obj);
    }
    obj.SetActive(true);
    if (PoolMgr.isOpenLayout)
        obj.transform.SetParent(null);

    return obj;
}
```
## 1. 存在空闲对象
```text
dataStack.Pop()
       ↓
加入 usedList
       ↓
SetActive(true)
```
## 2. 已达上限且没有空闲对象
```text
取出 usedList[0]
        ↓
从列表头部删除
        ↓
重新加入列表尾部
        ↓
作为最新使用的对象
```
这一过程不会创建新对象，只会更新对象的使用顺序。

# 九、对象回收
```csharp
public void Push(GameObject obj)
{
    obj.SetActive(false);
    if (PoolMgr.isOpenLayout)
        obj.transform.SetParent(rootObj.transform);

    dataStack.Push(obj);
    usedList.Remove(obj);
}
```
归还对象时需要完成四件事：
1. 使用 `SetActive(false)` 隐藏对象。
2. 开启布局管理时，把对象放回对应的抽屉节点。
3. 将对象压入 `dataStack`，等待下次复用。
4. 从 `usedList` 中移除，表示对象已不再使用。
管理器通过对象名称找到对应的池：
```csharp
public void PushObj(GameObject obj)
{
    poolDic[obj.name].Push(obj);
}
```

# 十、对象的完整生命周期
首次获取：
```text
GetObj
  ↓
对象池不存在
  ↓
Resources.Load + Instantiate
  ↓
加入 usedList
  ↓
开始使用
```
正常归还：
```text
PushObj
  ↓
SetActive(false)
  ↓
从 usedList 移除
  ↓
压入 dataStack
```
再次获取：
```text
dataStack.Pop()
  ↓
加入 usedList
  ↓
SetActive(true)
  ↓
继续使用
```
达到上限后的获取：
```text
dataStack 为空
  ↓
UsedCount >= maxNum
  ↓
取出 usedList[0]
  ↓
重置并复用
```

# 十二、上限优化前后的区别

| 对比项 | 无数量上限 | 有数量上限 |
|---|---|---|
| 空闲池为空 | 创建新对象 | 未达上限才创建 |
| 达到最大数量 | 继续创建 | 复用最早使用的对象 |
| 对象数量 | 可能持续增长 | 基本保持在设定范围内 |
| 内存和渲染压力 | 难以控制 | 更容易控制 |
| 使用中对象被抢占 | 不会 | 达到上限时可能发生 |

# 十三、PoolData.cs
```
using System.Collections.Generic;
using UnityEngine;

public class PoolData
{
    // 抽屉的父物体
    private readonly GameObject objRoot;

    // 当前没有使用的对象
    private readonly Stack<GameObject> objStack = new();

    // 当前正在使用的对象
    // 越靠近列表头部，表示开始使用的时间越早
    private readonly List<GameObject> usedList = new();

    // 空闲对象数量
    public int Count => objStack.Count;

    // 使用中对象数量
    public int UsedCount => usedList.Count;

    /// <summary>
    /// 创建某一类对象对应的抽屉。
    /// 创建抽屉时产生的第一个对象正在使用，因此需要记录到 usedList 中。
    /// </summary>
    public PoolData(GameObject poolRoot, string poolName, GameObject firstUsedObj)
    {
        if (PoolMgr2.Instance.IsOpenLayout)
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

        if (PoolMgr2.Instance.IsOpenLayout)
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

        if (PoolMgr2.Instance.IsOpenLayout)
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
```

# 十四、PoolMgr2.cs
`PoolMgr2` 负责创建对象、获取对象、回收对象以及管理不同类型的抽屉。
```
using System.Collections.Generic;
using UnityEngine;

public class PoolMgr2 : Singleton2<PoolMgr2>
{
    // 柜子：Key 是对象类型，Value 是对应的抽屉
    private readonly Dictionary<string, PoolData> poolDic = new();

    // 对象池在 Hierarchy 中的总根物体
    private GameObject poolRoot;

    // 是否开启 Hierarchy 层级整理
    public bool IsOpenLayout = true;

    private PoolMgr2()
    {
    }

    /// <summary>
    /// 从对象池获取对象。
    /// </summary>
    /// <param name="name">Resources 文件夹下的资源路径。</param>
    /// <param name="maxNum">这一类对象允许存在的最大数量。</param>
    public GameObject GetObj(string name, int maxNum = 50)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("对象池资源路径不能为空");
            return null;
        }

        // 至少允许创建一个对象
        maxNum = Mathf.Max(1, maxNum);

        // 情况一：还没有这种对象对应的抽屉
        if (!poolDic.TryGetValue(name, out PoolData poolData))
        {
            GameObject newObj = CreateObj(name);

            if (newObj == null)
                return null;

            // PoolData 创建层级节点前，必须保证总根物体已经存在
            CreatePoolRoot();

            poolData = new PoolData(poolRoot, name, newObj);
            poolDic.Add(name, poolData);

            return newObj;
        }

        // 情况二：抽屉中存在空闲对象，直接复用
        if (poolData.Count > 0)
        {
            return poolData.Pop();
        }

        // 情况三：没有空闲对象，但还没有达到数量上限
        if (poolData.UsedCount < maxNum)
        {
            GameObject newObj = CreateObj(name);

            if (newObj == null)
                return null;

            poolData.PushUsedList(newObj);
            return newObj;
        }

        // 情况四：没有空闲对象，并且已经达到数量上限
        // Pop 会取出 usedList[0]，即使用时间最久的对象
        return poolData.Pop();
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
        if (!poolDic.TryGetValue(obj.name, out PoolData poolData))
        {
            poolData = new PoolData(poolRoot, obj.name, obj);
            poolDic.Add(obj.name, poolData);
        }

        poolData.Push(obj);
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

        // 去掉实例化后自动添加的 (Clone)，方便归还时查找对象池
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
```

# 十四、Lesson12MainTest.cs
连续点击鼠标左键获取方块。最多创建 `maxNum` 个方块；达到上限后，会重新使用最早获取的方块。

```csharp
using UnityEngine;

public class Lesson12MainTest : MonoBehaviour
{
    [SerializeField] private string resourcePath = "Prefabs/Cube";
    [SerializeField] private int maxNum = 5;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject obj = PoolMgr2.Instance.GetObj(resourcePath, maxNum);

            if (obj == null)
                return;

            // 对象可能是从池中取出的，也可能是被抢占的旧对象
            // 因此每次获取后都应重新设置本轮使用需要的状态
            obj.transform.position = Vector3.zero;
            obj.transform.rotation = Quaternion.identity;
        }
    }
}
```