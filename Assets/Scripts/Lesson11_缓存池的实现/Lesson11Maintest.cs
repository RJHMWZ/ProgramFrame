using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lesson11Maintest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PoolMgr.Instance.GetObj("Prefabs\\Cube");
        }
    }
}
