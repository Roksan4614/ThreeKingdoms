using UnityEngine;

public class Character_Enemy : CharacterComponent
{
    public bool isBoss { get; protected set; }

    public override void SetHeroData(string _key)
    {
        SetHeroData_Enemy(_key);
    }

    void SetHeroData_Enemy(string _key, GradeType _gradeType = GradeType.Normal, int _enchantLevel = 0)
    {
        if (_key.IsActive())
            m_stat = TableManager.statHero.GetStatData(_key, _gradeType, _enchantLevel);

        if (m_stat.isActive == false)
        {
            if (_key.IsActive())
                m_stat = TableManager.statEnemy.GetStatData(_key, _gradeType, _enchantLevel);

            if (m_stat.isActive == false)
                m_stat = TableManager.statEnemy.GetStatData("Enemy", _gradeType, _enchantLevel);
        }
        SetFaction(FactionType.Enemy);

        if (isBoss)
            SetActive_HP(false);
    }

    public void SetHeroData_Stage(string _key)
    {
        var stageData = StageManager.instance.data;

        SetHeroData_Enemy(_key);
        if (stageData.isBossWait)
        {
            SetBuffStat(0.1f);
        }
        else
        {
            float percent = Mathf.Pow(2f, stageData.level - 1);

            int progress = (stageData.chapterNumber - 1) * 10 + stageData.stageNumber - 1;
            percent *= Mathf.Pow(2f, progress * 0.01f);

            if (isBoss == false)
                percent *= 0.4f;

            SetBuffStat(percent);
        }
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

        m_element.rtHP.SetAnchoredPositionX(0);
        SetActive_HP(true);
    }
}
