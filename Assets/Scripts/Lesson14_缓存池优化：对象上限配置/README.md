# Lesson14：缓存池优化——对象上限配置

## 一、本课目标

Lesson13 通过 `GetObj` 的参数设置对象数量上限：

```csharp
PoolMgr3.Instance.GetObj("Prefabs/Cube", 5);
```

这种方式要求调用者每次获取对象时都知道并传入正确的上限。Lesson14 将最大数量配置放到预制体自身，让每种对象独立保存自己的配置：

```csharp
PoolMgr4.Instance.GetObj("Prefabs/Cube");
```

缓存池创建对象时读取预制体上的 `PoolObj.MaxNum`，调用者只需要提供资源路径。

## 二、为什么把上限配置放到对象上

不同类型的对象通常需要不同的数量上限，例如：

| 对象类型 | 建议上限 | 原因 |
|---|---:|---|
| 子弹 | 50 | 生成频率高、生命周期短 |
| 敌人 | 20 | 同屏数量有限，但单个对象开销较大 |
| 爆炸特效 | 10 | 存在时间短，可重复利用 |

如果由调用者传入上限，同一种对象可能在不同位置得到不同配置。把配置保存在预制体上具有以下优点：

- 配置与对象放在一起，更容易查找和修改。
- 每种预制体可以拥有独立的最大数量。
- 获取对象的接口更简单，不需要重复传入 `maxNum`。
- 策划或开发者可以直接在 Inspector 中调整数值。

## 三、PoolObj 配置组件

`PoolObj` 是挂载在缓存对象预制体上的配置脚本：

```csharp
using UnityEngine;

public class PoolObj : MonoBehaviour
{
    public int MaxNum = 5;
}
```

`MaxNum` 表示这一类对象最多允许同时创建多少个实例。因为它是公共字段，所以可以直接在 Unity Inspector 中修改。

本课中的 `Cube.prefab` 已挂载 `PoolObj`，并将 `MaxNum` 设置为 `5`。

## 四、使用步骤

1. 选中需要放入缓存池的预制体。
2. 为预制体添加 `PoolObj` 组件。
3. 在 Inspector 中设置 `Max Num`。
4. 为对象添加对应的自动回收脚本，例如 `HideMe4`。
5. 通过 `PoolMgr4.Instance.GetObj(resourcePath)` 获取对象。

示例：

```csharp
if (Input.GetMouseButtonDown(0))
{
    PoolMgr4.Instance.GetObj("Prefabs/Cube");
}
```

资源必须位于 `Resources` 文件夹下。传入的是相对路径，不需要扩展名。Unity 中推荐使用正斜杠：

```csharp
PoolMgr4.Instance.GetObj("Prefabs/Cube");
```

## 五、读取对象上限配置

`PoolMgr4.CreateObj` 在实例化对象后获取它的 `PoolObj` 组件：

```csharp
private GameObject CreateObj(string name)
{
    GameObject prefab = Resources.Load<GameObject>(name);
    if (prefab == null)
    {
        Debug.LogError($"Resources 中没有找到对象：{name}");
        return null;
    }

    GameObject obj = Object.Instantiate(prefab);
    PoolObj poolObj = obj.GetComponent<PoolObj>();

    if (poolObj != null)
    {
        maxNum = poolObj.MaxNum;
    }
    else
    {
        Debug.LogError("缓存池物体没有挂载 PoolObj");
        return null;
    }

    obj.name = name;
    return obj;
}
```

处理流程如下：

```text
加载并实例化预制体
        ↓
获取 PoolObj 组件
        ↓
读取 MaxNum
        ↓
保存资源路径作为对象名称
        ↓
返回创建完成的对象
```

如果预制体没有挂载 `PoolObj`，管理器会输出错误并返回 `null`。因此，所有交给 `PoolMgr4` 创建的预制体都必须添加该组件。

## 六、获取对象时的四种情况

`PoolMgr4.GetObj` 不再接收 `maxNum` 参数，而是使用从 `PoolObj` 中读取的配置：

```csharp
public GameObject GetObj(string name)
```

获取对象时分为四种情况：

| 情况 | 处理方式 |
|---|---|
| 对应的对象池还不存在 | 创建第一个对象和对应的 `PoolData3` |
| 存在空闲对象 | 从 `objStack` 中取出并复用 |
| 没有空闲对象，但未达到 `MaxNum` | 创建新对象并加入 `usedList` |
| 没有空闲对象，并且达到 `MaxNum` | 复用 `usedList` 中最早使用的对象 |

核心判断：

```csharp
if (poolData.Count > 0)
{
    return poolData.Pop();
}

if (poolData.UsedCount < maxNum)
{
    GameObject newObj = CreateObj(name);
    poolData.PushUsedList(newObj);
    return newObj;
}

return poolData.Pop();
```

这样可以保证对象数量达到上限后不再继续实例化。

## 七、PoolData3 的职责

`PoolData3` 管理同一类型对象的空闲状态和使用顺序：

```csharp
public Stack<GameObject> objStack = new();
private List<GameObject> usedList = new();

public int Count => objStack.Count;
public int UsedCount => usedList.Count;
```

- `objStack`：保存已经回收、当前空闲的对象。
- `usedList`：按开始使用的先后顺序保存正在使用的对象。
- `Count`：空闲对象数量。
- `UsedCount`：使用中对象数量。

达到数量上限且没有空闲对象时，会取出 `usedList[0]`，也就是使用时间最久的对象：

```csharp
obj = usedList[0];
usedList.RemoveAt(0);
usedList.Add(obj);
```

将它重新添加到列表尾部，表示该对象开始了新一轮使用。

## 八、对象回收

`HideMe4` 在对象启用两秒后将其归还给 `PoolMgr4`：

```csharp
public class HideMe4 : MonoBehaviour
{
    private void OnEnable()
    {
        Invoke(nameof(HideSelf), 2f);
    }

    private void HideSelf()
    {
        PoolMgr4.Instance.PushObj(gameObject);
    }
}
```

回收时，`PoolData3.Push` 会：

1. 将对象设置为不激活状态。
2. 开启布局功能时，将对象移动到对应的分类节点下。
3. 把对象压入空闲栈 `objStack`。
4. 从使用列表 `usedList` 中移除。

## 九、完整生命周期

```text
首次获取
  ↓
创建对象并读取 PoolObj.MaxNum
  ↓
加入 usedList
  ↓
对象开始使用
  ↓
调用 PushObj 回收
  ↓
从 usedList 移除并压入 objStack
  ↓
再次获取时从 objStack 取出
```

达到数量上限后的流程：

```text
objStack 为空
  ↓
UsedCount >= MaxNum
  ↓
取出 usedList[0]
  ↓
移动到列表尾部
  ↓
重新使用，不再创建新对象
```

## 十、与 Lesson13 的区别

| 对比项 | Lesson13 | Lesson14 |
|---|---|---|
| 上限来源 | 调用 `GetObj` 时传入 | 从预制体的 `PoolObj` 读取 |
| 调用方式 | `GetObj(path, maxNum)` | `GetObj(path)` |
| 配置位置 | 调用代码 | 预制体 Inspector |
| 调整方式 | 修改调用参数 | 修改 `PoolObj.MaxNum` |
| 调用者职责 | 知道对象上限 | 只需要知道资源路径 |

## 十一、注意事项

- `MaxNum` 应设置为大于 `0` 的数值，否则对象池无法正常扩充。
- 每个通过 `PoolMgr4` 创建的预制体都必须挂载 `PoolObj`。
- 对象回收脚本必须调用 `PoolMgr4`，不能混用 lesson12 或 lesson13 的管理器。
- 对象名称会被改为资源路径，用作 `poolDic` 的键，不要在使用期间随意修改。
- 达到上限后会抢占最早使用的对象，因此每次获取后都应重新设置位置、旋转、动画等运行状态。
- 当前 `maxNum` 是 `PoolMgr4` 的共享字段，适合本课单一预制体测试。若同时管理多种不同上限的对象，更合理的做法是把上限保存在各自的 `PoolData3` 中。

