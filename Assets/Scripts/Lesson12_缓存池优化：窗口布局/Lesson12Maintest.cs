using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lesson12Maintest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PoolMgr2.Instance.GetObj("Prefabs\\Cube");
        }
    }
}
