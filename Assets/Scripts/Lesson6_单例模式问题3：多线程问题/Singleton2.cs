using System;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class Singleton2<T>  where T:class
{
    private static T instance;
    private static readonly System.Object lockInstance=new System.Object();
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lockInstance)
                {
                    if (instance == null)
                    {
                        CreateInstance();
                    }
                }
            }
            return instance;
        }
    }

    private static T CreateInstance()
    {
        Type type=typeof(T);
        ConstructorInfo constructorInfo=type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null
                );
        if (constructorInfo == null)
        {
            throw new InvalidOperationException($"{typeof(T).Name} 必须拥有私有无参构造函数");
        }
        return constructorInfo.Invoke(null) as T;
    }
}
