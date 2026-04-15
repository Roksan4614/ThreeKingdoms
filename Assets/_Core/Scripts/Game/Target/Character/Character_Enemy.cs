using UnityEngine;

public class Character_Enemy : CharacterComponent
{
    public bool isBoss { get; protected set; }

    public override void SetHeroData(string _key = null)
    {
        if (_key.IsActive())
            m_stat = TableManager.statEnemy.GetStatData(_key);

        if (m_stat.isActive == false)
            m_stat = TableManager.statEnemy.GetStatData("Enemy");

        var stageData = StageManager.instance.data;

        float percent = (float)(stageData.level + GradeType.NONE + 1);
        percent += (stageData.chapterNumber - 1) * 0.1f;
        percent += (stageData.stageNumber - 1) * 0.1f;
        SetBuffStat(percent);

        if (stageData.isBossWait)
            SetBuffStat(0.1f);
        if (isBoss == false)
            SetBuffStat(0.5f);

        SetFaction(FactionType.Enemy);
    }

    public void SetBuffStat(float _percent)
    {
        m_stat.attackPower *= _percent;
        m_stat.health = m_stat.healthMax = m_stat.healthMax * _percent;
    }

    public void SetBossData(string _key = null)
    {
        if (_key.IsActive())
            m_stat = TableManager.statEnemy.GetStatData(_key);

        if (m_stat.isActive == false)
            m_stat = TableManager.statEnemy.GetStatData("Enemy");

        SetBuffStat(2);
        m_stat.health = m_stat.healthMax = m_stat.healthMax * 2;

        SetFaction(FactionType.Enemy);
    }
}
