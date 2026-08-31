using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lesson22Main : MonoBehaviour
{
   private void Start()
    {
        // 第一次请求 Test：开启一个异步加载协程
        ResMgr2.Instance.LoadAsync<GameObject>(
            "Prefabs\\Cube",
            prefab =>
            {
                if (prefab != null)
                {
                    Instantiate(prefab);
                    Debug.Log("第一个回调执行");
                }
            }
        );

        // 第二次请求相同资源：
        // 不会开启新协程，只会将回调追加到委托中
        ResMgr2.Instance.LoadAsync<GameObject>(
            "Prefabs\\Cube",
            prefab =>
            {
                if (prefab != null)
                {
                    Instantiate(prefab);
                    Debug.Log("第二个回调执行");
                }
            }
        );
    }
}
