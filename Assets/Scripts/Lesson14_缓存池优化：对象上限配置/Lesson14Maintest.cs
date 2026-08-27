using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lesson14Maintest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PoolMgr4.Instance.GetObj("Prefabs\\Cube");
        }
    }
}
