using UnityEngine;

public class Character_Enemy : CharacterComponent
{
    public bool isBoss { get; protected set; }

    public override void SetHeroData(string _key)
    {
        m_data = TableManager.enemy.GetHeroData(_key);

        if (m_data.isActive == false)
            m_data = TableManager.enemy.GetHeroData("Enemy");

        var stageData = StageManager.instance.data;

        float percent = (float)(stageData.level + GradeType.NONE + 1);
        percent += (stageData.chapterIdx - 1) * 0.1f;
        percent += (stageData.stageIdx - 1) * 0.1f;
        SetBuffStat(percent);

        if (stageData.isBossWait)
            SetBuffStat(0.1f);
        if (isBoss == false)
            SetBuffStat(0.5f);

        SetFaction(FactionType.Enemy);

        transform.SetParent(MapManager.instance.element.pEnemy);
    }

    public void SetBuffStat(float _percent)
    {
        m_data.attackPower = (int)(m_data.attackPower * _percent);
        m_data.health = m_data.healthMax = (int)(m_data.healthMax * _percent);
    }
}
