# 一、懒汉式单例
## 1. 概念
懒汉式单例（Lazy Singleton）的核心是：**第一次真正访问单例时才创建实例。**
## 2. 基本写法
```
private static T instance;
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
特点：
```
不使用 → 不创建
第一次使用 → 创建
之后使用 → 返回已有实例
```
## 3. 优点
- 延迟创建对象
- 不使用该功能时不会创建对应实例
- 适合初始化成本较高、可能不会使用的系统
某些功能如果整个游戏流程都没有用到，就没有必要提前初始化。
懒加载的主要价值不仅是“节省一个对象的内存”，更重要的是==**推迟对象初始化及相关资源、数据和系统的加载成本**==。

# 二、懒汉式的线程安全实现
前面使用过双重检查锁：
```
private static readonly object instanceLock = new();
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

# 三、Lazy\<T>
对于普通 C# 对象，如果只是需要实现线程安全的延迟初始化，可以使用 .NET 提供的：
```
Lazy<T>
```
例如：
```
private static readonly Lazy<GameManager> instance =new(() => new GameManager());

public static GameManager Instance => instance.Value;
```
`Lazy<T>` 会负责：
- 延迟初始化
- 缓存创建后的实例
- 默认提供线程安全的初始化机制
因此相比自己手写复杂的：
```
if
lock
if
```
如果场景合适，`Lazy<T>` 通常更加清晰。

# 四、饿汉式单例
## 1. 概念
饿汉式单例（Eager Singleton）的核心是：
==**在类型初始化过程中直接创建单例，而不是等访问 Instance 时再判断创建。**==
典型写法：
```
public sealed class GameManager
{
    private static readonly GameManager instance = new();
    public static GameManager Instance => instance;
    private GameManager()
    {
    }
}
```
这里没有：if (instance == null)
也没有：lock
实例由静态字段初始化完成。

# 五、饿汉式什么时候创建
在 C# 中，更准确的理解是：**当运行时需要初始化这个类型时，静态字段才会进行初始化。**
例如第一次真正使用：GameManager.Instance时，会触发类型初始化。
## 1. typeof 不会因为获取 Type 就创建单例
例如：
```
Type type = typeof(GameManager);
```
单纯获取类型信息，不能简单认为一定会执行：
```
new GameManager();
```
## 2. 加载程序集也不等于创建所有静态实例
程序集被加载：
```
Assembly
```
并不意味着：
```
其中所有类型
↓
全部执行静态初始化
↓
全部创建单例
```
类型初始化由 CLR 根据类型实际使用情况进行。
更准确的是：
```
饿汉=类型初始化时创建
懒汉=第一次访问单例时创建
```

# 六、为什么饿汉式天然具有初始化线程安全
例如：
```
private static readonly GameManager instance = new();
```
静态字段的初始化由 CLR 管理。
CLR 会保证类型初始化的线程安全：==多个线程同时第一次使用该类型时，不会分别执行多次静态初始化。==
因此不需要自己再写：lock来保护：new GameManager();

# 七、饿汉式不代表整个对象都线程安全
例如：
```
public sealed class DataManager
{
    private static readonly DataManager instance = new();
    public static DataManager Instance => instance;
    private readonly List<string> data = new();
    private DataManager()
    {
    }

    public void Add(string value)
    {
        data.Add(value);
    }
}
```

```
data.Add()
```
如果被多个线程同时调用，仍然可能产生线程安全问题。

# 八、懒汉式与饿汉式区别

|对比|懒汉式|饿汉式|
|---|---|---|
|创建时机|第一次访问 Instance 时|类型初始化时|
|延迟初始化|是|否|
|判空|通常需要|不需要|
|多线程初始化|需要考虑|CLR 保证静态初始化安全|
|实现复杂度|相对高|简单|
|适合场景|初始化昂贵或可能不用|一定会使用、初始化成本低|

# 九、如何选择
两种方式解决的是不同需求。如果对象：
- 一定会使用
- 创建成本很低
- 希望代码简单
- 不需要延迟初始化
可以使用静态字段直接初始化：
```
private static readonly GameManager instance = new();
```
如果对象：
- 可能不会使用
- 初始化成本较高
- 希望真正使用时才创建
使用延迟初始化更加合适。可以使用：
```
Lazy<T>
```
或者根据框架需求实现自己的懒加载逻辑。
两者真正的区别主要是：**实例的初始化时机不同。**