using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideMe : MonoBehaviour
{
    void OnEnable()
    {
        Invoke("HideSelf",2f);
    }

    private void HideSelf()
    {
        PoolMgr.Instance.saveObj(gameObject);
    }
}
