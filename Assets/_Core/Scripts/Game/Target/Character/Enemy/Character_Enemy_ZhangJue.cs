using UnityEngine;

public class Character_Enemy_ZhangJue : Character_Enemy
{
    protected override void Awake()
    {
        isBoss = true;
        base.Awake();
    }


    public override bool OnDamage(int _damage)
    {
        var result = base.OnDamage(_damage);
        Signal.instance.UpdageBossHP.Emit(isLive ?  m_data.health / (float)m_data.healthMax : 0);

        // 보스가 죽었기 때문에 다 죽이자!!
        if(isLive == false)
            StageManager.instance.BossKillAllDieEnemy();

        return result;
    }
}
