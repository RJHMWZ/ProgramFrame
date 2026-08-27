using UnityEngine;

public class Player2 : MonoBehaviour
{
    private void Awake()
    {
        EventMgr2.Instance.AddEventListener(
            EventMgr2.enemyDieEvent,
            UpGrade
        );
    }

    // 带参数事件的监听方法必须接收一个 object
    private void UpGrade(object info)
    {
        Enemy2 enemy = info as Enemy2;

        if (enemy == null)
        {
            Debug.LogError("敌人死亡事件传入的数据不是 Enemy");
            return;
        }

        Debug.Log(
            $"击败编号为 {enemy.enemyID} 的敌人，人物升级"
        );
    }

    private void OnDestroy()
    {
        EventMgr2.Instance.RemoveEventListener(
            EventMgr2.enemyDieEvent,
            UpGrade
        );
    }
}