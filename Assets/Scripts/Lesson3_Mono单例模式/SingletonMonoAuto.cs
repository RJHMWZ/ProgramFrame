using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonMonoAuto<T>: MonoBehaviour where T:MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj=new GameObject(typeof(T).Name);
                instance=obj.AddComponent<T>();
                DontDestroyOnLoad(obj);//过场景不消失
            }
            return instance;
        }
    }
}
