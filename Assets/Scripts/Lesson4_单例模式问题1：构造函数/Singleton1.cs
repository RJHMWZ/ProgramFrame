using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Singleton1<T> where T:class
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                Type type=typeof(T);
                ConstructorInfo constructorInfo=type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null
                );
                if (constructorInfo != null)
                {
                    instance=constructorInfo.Invoke(null) as T;
                }
            }
            return instance;
        }
    }
}
