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

    public void StartStage()
    {
        for (int i = 0; i < m_lstHeroInfo.Count; i++)
            m_lstHeroInfo[i].StartStage();
    }

    public void UpdateHP(CharacterComponent _hero)
    {
        if (_hero.teamPosition > TeamPositionType.NONE)
            m_lstHeroInfo.Find(x => x.key == _hero.data.key).UpdateHP();
    }

    public void StopRespawn(CharacterComponent _hero)
    {
        if (_hero.teamPosition > TeamPositionType.NONE)
            m_lstHeroInfo.Find(x => x.key == _hero.data.key).StopRespawn();
    }

    public void StopRespawn()
    {
        for (int i = 0; i < m_lstHeroInfo.Count; i++)
            m_lstHeroInfo[i].StopRespawn();
    }
}
