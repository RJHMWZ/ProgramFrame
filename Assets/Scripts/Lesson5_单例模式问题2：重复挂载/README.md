# 一、单例模式重复挂载问题
## 1. 重复挂载会破坏单例唯一性
`MonoBehaviour` 单例依附在 `GameObject` 上，因此可能出现多个相同单例组件。

# 二、禁止同一 GameObject 重复挂载
可以给单例基类添加：\[DisallowMultipleComponent](==治标不治本==)
例如：
```
[DisallowMultipleComponent]
public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
{
}
```
==它**不能解决不同 GameObject 之间的重复问题**。==

# 三、运行时检测重复单例
真正保证唯一性的逻辑应该放在 `Awake()` 中。
```
protected virtual void Awake()
{
    if (instance != null && instance != this)
    {
        Destroy(gameObject);
        return;
    }
    instance = this as T;
    DontDestroyOnLoad(gameObject);
}
```

# 四、为什么判断 instance != this
已经存在实例，并且这个实例不是当前对象时，才属于重复对象。

# 五、Destroy(this) 与 Destroy(gameObject)
Destroy(this)它只销毁当前单例组件，GameObject 本身仍然存在。
Destroy(gameObject);直接删除整个重复对象：
```
if (instance != null && instance != this)
{
    Destroy(gameObject);
    return;
}
```

# 六、完整的 MonoBehaviour 单例基类
可以将重复挂载处理直接封装进基类：
```
using UnityEngine;

[DisallowMultipleComponent]
public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
{
    private static T instance;
    public static T Instance => instance;
    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
```

# 七、OnDestroy 清除 Instance
```
protected virtual void OnDestroy()
{
    if (instance == this)
    {
        instance = null;
    }
}
```
作用：当前真正的单例被销毁时，同时清空静态引用。
让单例状态和实际对象生命周期保持一致。注意一定要判断：
```
instance == this
```
避免重复对象销毁时把真正的单例引用清掉。

# 八、自动创建单例的重复问题
如果使用自动创建方式：
```
if (instance == null)
{
    GameObject obj = new GameObject(typeof(T).Name);
    instance = obj.AddComponent<T>();
    DontDestroyOnLoad(obj);
}
```
核心规则：==自动创建的单例就不要再手动放进场景。==

# 九、单例重复挂载的处理方式
第一层DisallowMultipleComponent
第二层Awake 中判断 Instance