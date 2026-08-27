using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lesson13Maintest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PoolMgr3.Instance.GetObj("Prefabs\\Cube");
        }
    }
}
