using UnityEngine;

public class Level3 : MonoBehaviour
{
    private void Awake()
    {
        EventMgr3.Instance.AddEventListener<Enemy3>(
            EventMgr3.enemyDieEvent,
            ClearLevel
        );
    }

    private void ClearLevel(Enemy3 enemy)
    {
        Debug.Log(
            $"击败敌人：{enemy.enemyName}，游戏成功通关"
        );
    }

    private void OnDestroy()
    {
        EventMgr3.Instance.RemoveEventListener<Enemy3>(
            EventMgr3.enemyDieEvent,
            ClearLevel
        );
    }
}