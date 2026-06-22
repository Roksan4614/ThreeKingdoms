using UnityEngine;

public class Character_Enemy : CharacterComponent
{
    public bool isBoss { get; protected set; }

    public override void SetHeroData(string _key = null)
    {
        if (_key.IsActive())
            m_stat = TableManager.statHero.GetStatData(_key);

        if (m_stat.isActive == false)
        {
            if (_key.IsActive())
                m_stat = TableManager.statEnemy.GetStatData(_key);

            if (m_stat.isActive == false)
                m_stat = TableManager.statEnemy.GetStatData("Enemy");
        }

        var stageData = StageManager.instance.data;


        if (stageData.isBossWait)
        {
            SetBuffStat(0.1f);
        }
        else
        {
            float percent = (float)(stageData.level + GradeType.NONE + 1);
            percent += (stageData.chapterNumber - 1) * 0.1f;
            percent += (stageData.stageNumber - 1) * 0.1f;
            if (isBoss == false)
                percent *= 0.5f;

            SetBuffStat(percent);
        }

        SetFaction(FactionType.Enemy);
    }

    public void SetBuffStat(float _percent, bool _isAttackPower = true, bool _isHealth = true, bool _isDefence = true)
    {
        if (_isAttackPower)
            m_stat.attackPower *= _percent;
        if (_isHealth)
            m_stat.health = m_stat.healthMax = m_stat.healthMax * _percent;
        if (_isDefence)
            m_stat.defenceValue *= _percent;
    }

    public virtual void SetBossData(string _key = null)
    {
        if (_key.IsActive())
            m_stat = TableManager.statEnemy.GetStatData(_key);

        if (m_stat.isActive == false)
            m_stat = TableManager.statEnemy.GetStatData("Enemy");

        SetBuffStat(2);
        m_stat.health = m_stat.healthMax = m_stat.healthMax * 2;

        SetFaction(FactionType.Enemy);
    }
    public override void Respawn(bool _isSetState = true)
    {
        move.MoveStop();
        attack.ResetFX();
        buff.RemoveAll();

        SetColorParts(Color.white);
    }
}
