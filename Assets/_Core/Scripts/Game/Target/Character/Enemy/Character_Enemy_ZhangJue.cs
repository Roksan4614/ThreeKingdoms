using UnityEngine;

public class Character_Enemy_ZhangJue : Character_Enemy
{
    protected override void Awake()
    {
        isBoss = true;
        base.Awake();
    }


    public override bool OnDamage(CharacterComponent _attacker, float _damage)
    {
        var result = base.OnDamage(_attacker, _damage);
        Signal.instance.UpdageBossHP.Emit(isLive ?  m_stat.health / (float)m_stat.healthMax : 0);

        // 보스가 죽었기 때문에 다 죽이자!!
        if(isLive == false)
            StageManager.instance.BossKillAllDieEnemy();

        return result;
    }
}
