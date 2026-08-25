# 一、单例模式
## 1. 什么是单例模式
单例模式（Singleton Pattern）是一种设计模式。
作用：保证一个类在程序运行期间只有一个实例，并提供统一访问入口。
例如游戏中的管理类：
- GameManager（游戏管理）
- AudioManager（音频管理）
- UIManager（UI管理）
- DataManager（数据管理）
- ResourceManager（资源管理）
这些对象通常整个游戏只需要一个。
## 2. 为什么需要单例模式
如果普通创建对象：
```
GameManager gm1 = new GameManager();
GameManager gm2 = new GameManager();
```
会产生多个管理对象。可能导致：
- 数据不统一
- 多个对象重复执行逻辑
- 增加内存开销

# 二、非 MonoBehaviour 单例模式
## 1. 为什么 Manager 不继承 MonoBehaviour
Unity 中继承 `MonoBehaviour` 的类：
- 需要挂载到 GameObject
- 由 Unity 管理生命周期
- 可以使用 Awake、Start、Update 等函数
例如：
```
public class Player : MonoBehaviour
{

}
```

## 2. 非 MonoBehaviour 单例特点
优点：
- 不需要创建 GameObject
- 不依赖 Unity 生命周期
- 代码结构更加清晰
- 适合程序框架层
缺点：
- 不能使用 Awake、Start、Update
- 需要自己管理生命周期

# 三、泛型单例基类
## 1. 为什么使用泛型基类
如果每个 Manager 都单独写单例：每个管理器都需要重复编写。
## ==2. 泛型单例基类实现==
```
public abstract class Singleton<T> where T : class, new()
{
    private static T instance;
    public static T Instance
    {
        get
        {
            instance ??= new T();
            return instance;
        }
    }
}
```

# 四、泛型约束
## 1. class 约束
```
where T : class
```
表示：T 必须是引用类型。
原因：单例对象需要通过实例引用管理。
## 2. new() 约束
```
where T : new()
```
表示：T 必须拥有无参数构造函数。因为内部需要：new T()创建对象。

# 五、Instance 属性
## 1. 作用
`Instance` 是单例访问入口。
第一次访问：
```
Instance
 ↓
判断对象是否存在
 ↓
不存在
 ↓
创建对象
 ↓
返回对象
```
之后访问：
```
Instance
 ↓
已有对象
 ↓
直接返回
```
## 2. 为什么使用属性
推荐：GameManager.Instance;
不推荐：GameManager.GetInstance();
原因：`Instance` 表示获取对象，更符合 C# 属性设计习惯。

# 六、具体 Manager 使用方式
## 1. 创建管理类
```
public class GameManager : Singleton<GameManager>
{

}
```
继承：Singleton\<GameManager\>
即可拥有：GameManager.Instance
## 2. 添加功能
```
public class AudioManager : Singleton<AudioManager>
{
    public void PlayMusic()
    {
        Debug.Log("播放音乐");
    }
}
```
调用：AudioManager.Instance.PlayMusic();

# 七、非 MonoBehaviour 单例注意事项
## 1. 不能使用 Unity 生命周期
不能：
```
Awake()
Start()
Update()
```
如果需要初始化：
可以提供Initialize()
例如：GameManager.Instance.Initialize();
## 2. 避免过度使用
单例虽然方便：
```
XXXManager.Instance
```
但是大量使用会造成：
- 类之间耦合增加
- 后期修改困难
只用于真正需要全局唯一的对象。