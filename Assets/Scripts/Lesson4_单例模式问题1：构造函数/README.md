# 一、C# 构造函数与单例模式安全性
## 1. 单例模式存在的问题
```
public class Singleton<T> where T : class, new()
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
通过new T()创建实例。但是这种方式存在一个问题：==外部代码可以直接 new 单例对象，破坏单例唯一性==

# 二、限制外部创建对象
## 1. 私有构造函数
==解决方式：将构造函数设置为private==
这样外部无法：new GameManager();
只能：GameManager.Instance来获取对象。

# 三、私有构造函数带来的问题
如果单例基类中：instance = new T();
要求：where T : new()
但是：私有构造函数不能满足 `new()` 约束，二者冲突了。

# 四、通过反射创建私有构造函数对象
==解决方式：取消new()约束，改为where T : class，然后通过反射调用私有构造函数。==
```
using System;
using System.Reflection;

public abstract class Singleton<T> where T : class
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if(instance == null)
            {
                Type type = typeof(T);
                ConstructorInfo constructor =type.GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        null,
                        Type.EmptyTypes,
                        null
                    );
                if(constructor != null)
                {
                    instance = constructor.Invoke(null) as T;
                }
            }
            return instance;
        }
    }
}
```

# 五、反射获取构造函数
## Type
获取类型信息：==typeof(T)==
## GetConstructor()
获取指定构造函数：type.GetConstructor();
参数：BindingFlags.Instance | BindingFlags.NonPublic
表示：实例构造函数 | private 构造函数
## Type.EmptyTypes
无参数构造函数。
## Invoke()
执行获取到的构造函数：
```
constructor.Invoke(null)
```

# 六、为什么单例需要私有构造函数
==单例设计目标：只能由内部创建，外部禁止创建==

# 七、注意事项
- 单例需要防止外部直接创建对象
- 私有构造函数可以保证唯一性
- 使用反射可以调用私有构造函数
- 反射相比直接 new 有额外性能开销，但单例创建次数极少，影响可以忽略
- 单例主要解决全局唯一对象管理问题，不应该所有类都使用