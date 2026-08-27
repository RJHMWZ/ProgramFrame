using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideMe3 : MonoBehaviour
{
    void OnEnable()
    {
        Invoke("HideSelf",2f);
    }

    private void HideSelf()
    {
        PoolMgr3.Instance.PushObj(gameObject);
    }
}
