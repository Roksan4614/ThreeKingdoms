using UnityEngine;

public class Character_Enemy : CharacterComponent
{
    public override void SetHeroData(string _key)
    {
        m_data = TableManager.enemy.GetHeroData(_key);

        if (m_data.isActive == false)
            m_data = TableManager.enemy.GetHeroData("Enemy");
        m_data.attackPower *= 3;
        m_data.health = m_data.healthMax = m_data.healthMax * 3;
        SetFaction(FactionType.Enemy);

        transform.SetParent(MapManager.instance.element.pEnemy);
    }
}
