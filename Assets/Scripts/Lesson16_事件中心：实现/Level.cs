using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    void Awake()
    {
        EventMgr.Instance.AddEventListener(EventMgr.enemyDieEvent,ClearLevel);
    }

    private void ClearLevel()
    {
        Debug.Log("游戏成功通关");
    }

    void OnDestroy()
    {
        EventMgr.Instance.RemoveEventListener(EventMgr.enemyDieEvent,ClearLevel);
    }
}
