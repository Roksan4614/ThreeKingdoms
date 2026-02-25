using UnityEngine;

public class Character_Enemy : CharacterComponent
{
    public bool isBoss { get; protected set; }

    public override void SetHeroData(string _key)
    {
        m_data = TableManager.enemy.GetHeroData(_key);

        if (m_data.isActive == false)
            m_data = TableManager.enemy.GetHeroData("Enemy");
        //m_data.attackPower *= 3;
        //m_data.health = m_data.healthMax = m_data.healthMax * 3;
        SetFaction(FactionType.Enemy);

        transform.SetParent(MapManager.instance.element.pEnemy);
    }

    public void SetDebuffStat(float _percent)
    {
        m_data.attackPower = (int)(m_data.attackPower * _percent);
        m_data.health = m_data.healthMax = (int)(m_data.healthMax * _percent);
    }
}
