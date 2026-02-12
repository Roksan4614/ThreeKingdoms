using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Team_HeroInfo
{
    List<HeroInfoComponent> m_lstHeroInfo = new();

    public Team_HeroInfo(Transform _heroInfo)
    {
        for (int i = 0; i < _heroInfo.childCount; i++)
        {
            m_lstHeroInfo.Add(_heroInfo.GetChild(i).GetComponent<HeroInfoComponent>());
        }
    }

    public void SetTeamPosition()
    {
        int i = 0;

        var members = TeamManager.instance.members.Values.OrderBy(_x => _x.teamPosition);

        foreach (var hero in members)
        {
            var heroInfo = m_lstHeroInfo[i];
            heroInfo.SetHeroInfo(hero);
            i++;
        }

        for (; i < m_lstHeroInfo.Count; i++)
            m_lstHeroInfo[i].Disable();
    }

    public void ChapterStart()
    {
        foreach (var hero in TeamManager.instance.members.Values)
            m_lstHeroInfo[(int)hero.teamPosition].StartCooltimeSkill();
    }

    public void UpdateHP(CharacterComponent _hero)
    {
        if (_hero.teamPosition > TeamPositionType.NONE)
            m_lstHeroInfo[(int)_hero.teamPosition].UpdateHP();
    }
}
