using UnityEngine;

public class Level2 : MonoBehaviour
{
    private void Awake()
    {
        EventMgr2.Instance.AddEventListener(EventMgr2.enemyDieEvent,ClearLevel);
    }

    // 带参数事件的监听方法必须接收一个 object
    private void ClearLevel(object info)
    {
        Enemy2 enemy = info as Enemy2;

        if (enemy == null)
        {
            Debug.LogError("敌人死亡事件传入的数据不是 Enemy");
            return;
        }

        Debug.Log(
            $"击败敌人：{enemy.enemyName}，游戏成功通关"
        );
    }

    private void OnDestroy()
    {
        EventMgr2.Instance.RemoveEventListener(
            EventMgr2.enemyDieEvent,
            ClearLevel
        );
    }
}