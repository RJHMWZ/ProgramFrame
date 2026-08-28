using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lesson21Main : MonoBehaviour
{
    private GameObject currentObj;
    private void Start()
    {
        // 泛型异步加载
        ResMgr.Instance.LoadAsync<GameObject>("Prefabs\\Cube",
        prefab =>
            {
                if (prefab == null)
                {
                    Debug.LogError(
                        "Resources 中没有找到 Cube"
                    );
                    return;
                }

                currentObj = Instantiate(prefab);
            }
        );

        // Type 异步加载
        ResMgr.Instance.LoadAsync(
            "Prefabs\\Cube",
            typeof(GameObject),
            asset =>
            {
                GameObject prefab = asset as GameObject;

                if (prefab == null)
                {
                    Debug.LogError(
                        "加载结果不是 GameObject"
                    );
                    return;
                }

                Instantiate(prefab);
            }
        );
    }

    private void Update()
    {
        // 按下空格测试同步加载
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameObject prefab =ResMgr.Instance.Load<GameObject>("Prefabs\\Cube");

            if (prefab != null)
            {
                Instantiate(prefab);
            }
        }

        // 按下 U 清理未使用资源
        if (Input.GetKeyDown(KeyCode.U))
        {
            ResMgr.Instance.UnloadUnusedAssets(() =>
            {
                Debug.Log("未使用资源清理完成");
            });
        }

        // 按下 D 销毁场景实例
        if (Input.GetKeyDown(KeyCode.D) &&currentObj != null)
        {
            Destroy(currentObj);
            currentObj = null;
        }
    }
}
