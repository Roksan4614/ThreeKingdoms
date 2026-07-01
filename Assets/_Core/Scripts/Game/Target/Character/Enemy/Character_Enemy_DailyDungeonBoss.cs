using UnityEngine;

public class Character_Enemy_DailyDungeonBoss : Character_Enemy_Boss
{
    public override void SetBossData(string _key = null)
    {
        m_stat = TableManager.statEnemy.GetStatData(_key);

        if (m_stat.isActive == false)
            m_stat = TableManager.statEnemy.GetStatData("Enemy");

        if (DataManager.dailyDungeon.curGradeType > GradeType.Normal)
            SetBuffStat((float)(DataManager.dailyDungeon.curGradeType) * 2f);

        SetFaction(FactionType.Enemy);
    }

    public override bool OnDamage(CharacterComponent _attacker, float _damage, bool _isCritical = false)
    {
        base.OnDamage(_attacker, _damage, _isCritical);

        if (m_stat.health <= 1)
        {
            DataManager.dailyDungeon.curGradeType++;

            SetBossData(m_stat.key);

            Signal.instance.UpdageBossHP.Emit(1);
            Signal.instance.DailyDungeonNextStep.Emit(DataManager.dailyDungeon.curGradeType);
        }

        return false;
    }

}
