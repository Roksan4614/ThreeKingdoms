using UnityEngine;

public class Character_Enemy_Boss : Character_Enemy
{
    public override void Awake()
    {
        isBoss = true;
        base.Awake();
    }

    public override bool OnDamage(CharacterComponent _attacker, float _damage, bool _isCritical = false)
    {
        var result = base.OnDamage(_attacker, _damage, _isCritical);
        Signal.instance.UpdageBossHP.Emit((isLive ? m_stat.health / (float)m_stat.healthMax : 0, m_stat.healthMax));

        // 보스가 죽었기 때문에 다 죽이자!!
        if (isLive == false)
            StageManager.instance.BossKillAllDieEnemy();

        return result;
    }
}
