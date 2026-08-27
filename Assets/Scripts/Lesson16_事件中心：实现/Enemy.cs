using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int Hp=5;
    private bool isDead;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)&&!isDead)
        {
            Hp--;
            Debug.Log("敌人扣血");
            if (Hp <= 0)
            {
                Die();
            }
        }
        
    }

    void Die()
    {
        isDead=true;
        Debug.Log("怪兽死亡");
        EventMgr.Instance.EventTrigger(EventMgr.enemyDieEvent);
    }
}
