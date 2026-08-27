using UnityEngine;

public class Player3 : MonoBehaviour
{
    private void Awake()
    {
        EventMgr3.Instance.AddEventListener<Enemy3>(
            EventMgr3.enemyDieEvent,
            UpGrade
        );
    }

    private void UpGrade(Enemy3 enemy)
    {
        Debug.Log(
            $"击败编号为 {enemy.enemyID} 的敌人，人物升级"
        );
    }

    private void OnDestroy()
    {
        EventMgr3.Instance.RemoveEventListener<Enemy3>(
            EventMgr3.enemyDieEvent,
            UpGrade
        );
    }
}