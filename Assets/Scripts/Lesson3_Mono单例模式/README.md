# 一、MonoBehaviour 单例模式
## 1. 为什么需要 MonoBehaviour 单例
这种单例无法使用 Unity 生命周期：但是 Unity 中很多管理类需要
- 挂载到 GameObject
- 使用生命周期函数
- 使用组件功能
这些类更适合使用：==继承 MonoBehaviour 的单例模式。==

# 二、MonoBehaviour 单例特点
## 1. 不能直接 new
==原因：==MonoBehaviour 对象必须由 Unity 创建，并且依附于 GameObject。
正确方式：gameObject.AddComponent\<GameManager\>();
## 2. 必须依附 GameObject
MonoBehaviour 生命周期由 Unity 管理。

# 三、基础 MonoBehaviour 单例基类
## 1. 实现方式
```
public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            return instance;
        }
    }

    protected virtual void Awake()
    {
        instance = this as T;
    }
}
```
==核心：通过 `Awake()` 获取当前组件实例。==

# 四、使用 MonoBehaviour 单例
## 1. 创建 Manager
```
public class GameManager : SingletonMono<GameManager>
{

}
```
继承：SingletonMono\<GameManager>之后拥有
```
GameManager.Instance
```
## 2. 调用
GameManager.Instance.StartGame();访问的是场景中唯一的 GameManager。

# 五、Awake 初始化注意事项
如果子类重写 Awake：
```
protected override void Awake()
{

}
```
必须调用：base.Awake();
原因：==父类 Awake 负责instance = this as T==;如果不调用,单例对象不会初始化。
示例：
```
public class GameManager : SingletonMono<GameManager>
{
    protected override void Awake()
    {
        base.Awake();
        //自己的初始化代码
    }
}
```

# 六、自动创建 MonoBehaviour 单例
## 1. 存在的问题
普通 MonoBehaviour 单例：==需要提前在场景中创建==
如果忘记创建：则为null
## 2. 自动创建方案
通过代码：
- 创建 GameObject并重命名
- 添加组件
- 保存实例
- 场景切换不销毁
```
public class SingletonAutoMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if(instance == null)
            {
                GameObject obj = new GameObject(typeof(T).Name);
                instance = obj.AddComponent<T>();
                DontDestroyOnLoad(obj);//切换场景时保留对象
            }
            return instance;
        }
    }
}
```

# 七、两种 MonoBehaviour 单例区别

|类型|创建方式|使用场景|
|---|---|---|
|普通 Mono 单例|手动挂载 GameObject|明确需要场景对象|
|自动 Mono 单例|代码自动创建|全局管理类|

# 八、普通单例与 MonoBehaviour 单例区别

|类型|基类|创建方式|生命周期|
|---|---|---|---|
|普通单例|class|new T()|C#管理|
|Mono 单例|MonoBehaviour|AddComponent|Unity管理|