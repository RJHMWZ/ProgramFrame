using UnityEngine;

public class Player4 : MonoBehaviour
{
    private void Awake()
    {
        EventMgr4.Instance.AddEventListener<Enemy4>(
            E_EventType4.EnemyDie,
            UpGrade
        );
    }

    private void UpGrade(Enemy4 enemy)
    {
        Debug.Log(
            $"击败编号为 {enemy.enemyID} 的敌人，人物升级"
        );
    }

    private void OnDestroy()
    {
        EventMgr4.Instance.RemoveEventListener<Enemy4>(
            E_EventType4.EnemyDie,
            UpGrade
        );
    }
}