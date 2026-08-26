using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SingletonMono1<T> : MonoBehaviour where T:MonoBehaviour
{
    private static T instance;
    public static T Instance=>instance;

    protected virtual void Awake()
    {
        if (instance == null&&instance!=this)
        {
            Destroy(gameObject);
            return;
        }
        instance=this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance=null;
        }
    }
}
