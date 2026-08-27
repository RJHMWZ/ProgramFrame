using UnityEngine;

public class Level4 : MonoBehaviour
{
    private void Awake()
    {
        EventMgr4.Instance.AddEventListener<Enemy4>(
            E_EventType4.EnemyDie,
            ClearLevel
        );
    }

    private void ClearLevel(Enemy4 enemy)
    {
        Debug.Log(
            $"击败敌人：{enemy.enemyName}，游戏成功通关"
        );
    }

    private void OnDestroy()
    {
        EventMgr4.Instance.RemoveEventListener<Enemy4>(
            E_EventType4.EnemyDie,
            ClearLevel
        );
    }
}