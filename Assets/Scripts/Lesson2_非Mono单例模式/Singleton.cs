/// <summary>
/// 适用于普通 C# 类的泛型单例。
/// 不要用它创建 MonoBehaviour；MonoBehaviour 必须由 Unity 创建和管理。
/// </summary>
/// <typeparam name="T">需要保证全局唯一的普通 C# 类型。</typeparam>
public class Singleton<T> where T : class, new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            instance??=new T();
            return instance;
        }
    }
}
