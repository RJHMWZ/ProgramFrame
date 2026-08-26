using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testmain : MonoBehaviour
{
    void Start()
    {
        Debug.Log("1111"+Test2.Instance.ToString());
        Test2.Instance.StartTest2Fun();
    }
}
