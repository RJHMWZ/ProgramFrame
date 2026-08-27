using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideMe4 : MonoBehaviour
{
    void OnEnable()
    {
        Invoke("HideSelf",2f);
    }

    private void HideSelf()
    {
        PoolMgr4.Instance.PushObj(gameObject);
    }
}
