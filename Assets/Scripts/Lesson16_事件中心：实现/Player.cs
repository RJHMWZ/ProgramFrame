using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    void Awake()
    {
        EventMgr.Instance.AddEventListener(EventMgr.enemyDieEvent,UpGrade);
    }

    private void UpGrade()
    {
        Debug.Log("人物升级");
    }

    void OnDestroy()
    {
        EventMgr.Instance.RemoveEventListener(EventMgr.enemyDieEvent,UpGrade);
    }
}
