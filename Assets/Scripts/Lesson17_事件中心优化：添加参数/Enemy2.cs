using UnityEngine;

public class Enemy2 : MonoBehaviour
{
    public string enemyName = "普通怪兽";
    public int enemyID = 1;
    public int Hp = 5;

    private bool isDead;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J) && !isDead)
        {
            Hp--;
            Debug.Log($"{enemyName}扣血，剩余生命值：{Hp}");

            if (Hp <= 0)
            {
                Die();
            }
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{enemyName}死亡");

        // 将当前死亡的 Enemy 作为参数传给事件中心
        EventMgr2.Instance.EventTrigger(
            EventMgr2.enemyDieEvent,
            this
        );
    }
}