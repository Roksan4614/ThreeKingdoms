using UnityEngine;

public class Character_Enemy : CharacterComponent
{
    public override void SetHeroData(string _key)
    {
        m_data = TableManager.hero.GetHeroData(_key);
        
        SetFaction(FactionType.Enemy);

        transform.SetParent(MapManager.instance.element.enemy);
    }
}
