using System.Collections.Generic;
using UnityEngine;

public class Team_HeroInfo
{
    List<HeroInfoComponent> m_lstHeroInfo = new();

    public Team_HeroInfo(Transform _heroInfo)
    {
        for( int i = 0; i < _heroInfo.childCount; i++ )
        {
            m_lstHeroInfo.Add(_heroInfo.GetChild(i).GetComponent<HeroInfoComponent>());
        }
    }

    public void SetTeamPosition()
    {
        int i = 0;

        var members = TeamManager.instance.members.Values;
        foreach( var hero in members)
        {
            m_lstHeroInfo[i].
            hero.
        }
    }
}
