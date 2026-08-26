# 一、单例模式的多线程问题
## 1. 什么是线程安全问题
当程序中存在多个线程，并且多个线程同时访问或修改同一份共享数据时，可能出现：
- 数据状态不一致
- 重复创建对象
- 修改结果丢失
- 集合读写异常
这种问题通常称为**多线程并发问题**或**线程安全问题**。

# 二、单例对象为什么可能被创建多次
```
public static T Instance
{
    get
    {
        if (instance == null)
        {
            instance = CreateInstance();
        }

        return instance;
    }
}
```
假设线程 A 和线程 B 同时第一次访问，两个线程都可能通过：instance == null的判断，从而分别创建对象。这就破坏了单例的唯一性。

# 三、lock
C# 可以使用：==lock==。保证同一时刻只有一个线程进入指定代码块。
基本写法：
```
private static readonly object instanceLock = new object();

lock (instanceLock)
{
    // 需要保护的代码
}
```
执行过程：
```
线程 A 获得锁
    ↓
执行代码
    ↓
释放锁

线程 B 等待
    ↓
获得锁
    ↓
执行代码
```
==`lock` 保护的是一段代码临界区，不是把整个对象“锁死”。==

# 四、单例创建加锁
```
private static readonly object instanceLock = new object();

public static T Instance
{
    get
    {
        lock (instanceLock)
        {
            if (instance == null)
            {
                instance = CreateInstance();
            }

            return instance;
        }
    }
}
```
这样即使多个线程同时第一次访问 `Instance`，也只有一个线程能够创建对象。但是每次访问 `Instance` 都需要进入 `lock`。而单例通常只有**第一次访问**需要创建对象，之后只是返回已有实例。

# 五、双重检查锁
可以先在锁外判断一次：
```
public static T Instance
{
    get
    {
        if (instance == null)
        {
            lock (instanceLock)
            {
                if (instance == null)
                {
                    instance = CreateInstance();
                }
            }
        }
        return instance;
    }
}
```
这里出现了两次：
```
if (instance == null)
```
所以称为：==Double-Checked Locking，双重检查锁==
## 1. 第一次判断
```
if (instance == null)
```
作用：如果单例已经创建，直接返回。绝大多数访问不需要进入 `lock`。
## 2. 第二次判断
```
lock (instanceLock)
{
    if (instance == null)
    {
        instance = CreateInstance();
    }
}
```

# 六、非 MonoBehaviour 单例的线程安全写法
结合前面私有构造函数和反射，可以把创建逻辑单独封装：
```
using System;
using System.Reflection;

public class Singleton<T> where T : class
{
    private static T instance;
    private static readonly object instanceLock = new object();
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = CreateInstance();
                    }
                }
            }
            return instance;
        }
    }

    private static T CreateInstance()
    {
        ConstructorInfo constructor = typeof(T).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null
        );

        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"{typeof(T).Name} 必须拥有私有无参构造函数"
            );
        }
        return (T)constructor.Invoke(null);
    }
}
```

# 七、共享数据加锁
```
private readonly object dataLock = new object();
public void AddData(object data)
{
    lock (dataLock)
    {
        dataList.Add(data);
    }
}
```

# 八、所有共享访问都要遵守同一套加锁规则
不能只给：AddData()加锁，却让其他地方直接访问集合。
例如：
```
public void AddData(object data)
{
    lock (dataLock)
    {
        dataList.Add(data);
    }
}
```
如果另外一个方法：
```
public void RemoveData(object data)
{
    dataList.Remove(data);
}
```
没有使用同一个锁，仍然不安全。
```
public void RemoveData(object data)
{
    lock (dataLock)
    {
        dataList.Remove(data);
    }
}
```
==只要操作的是同一份共享数据，就应该采用一致的同步策略。==

# 九、MonoBehaviour 单例是否需要加锁
对于继承 `MonoBehaviour` 的单例，一般**不要为了单例创建而加入多线程锁**。
原因：==Unity 中大量 API 都要求在主线程使用==
```
GameObject
Transform
Instantiate()
Destroy()
AddComponent()
```
MonoBehaviour 本身也是由 Unity 主线程管理的。因此正常情况下都运行在 Unity 主线程。
如果整个 MonoBehaviour 单例只从主线程访问，就不存在多个线程同时执行 `Awake()` 创建单例的问题。