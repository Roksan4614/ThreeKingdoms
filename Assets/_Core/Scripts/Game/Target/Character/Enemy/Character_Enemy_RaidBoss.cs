using DG.Tweening;
using UnityEngine;

public class Character_Enemy_RaidBoss : Character_Enemy
{

    //Vector3 m_prevPos;

    public override void SetBossData(string _key = null)
    {
        if (_key.IsActive())
            m_stat = TableManager.statHero.GetStatData(_key);

        if (m_stat.isActive == false)
        {
            m_stat = TableManager.statEnemy.GetStatData(_key);

            if (m_stat.isActive == false)
                m_stat = TableManager.statEnemy.GetStatData("Enemy");
        }

        SetBuffStat(((int)DataManager.bossRaid.data.nowGrade + 1) * 100, _isAttackPower: false);
        SetFaction(FactionType.Enemy);
    }

    public override bool OnDamage(CharacterComponent _attacker, float _damage, bool _isCritical = false)
    {
        if (isLive == false)
            return true;

        var result = base.OnDamage(_attacker, _damage, _isCritical);
        Signal.instance.UpdageBossHP.Emit(isLive ? m_stat.health / (float)m_stat.healthMax : 0);

        // todo
        if (isLive == false)
            BossRaidWorker.instance.Finish_Phase(this);

        return result;
    }
}
