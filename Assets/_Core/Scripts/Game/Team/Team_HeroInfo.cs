using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Team_HeroInfo
{
    List<HeroInfoComponent> m_lstHeroInfo = new();

    public Team_HeroInfo(Transform _heroInfo)
    {
        if (_heroInfo == null)
            return;

        for (int i = 0; i < _heroInfo.childCount; i++)
        {
            m_lstHeroInfo.Add(_heroInfo.GetChild(i).GetComponent<HeroInfoComponent>());
        }
    }

    public void DisableAll()
    {
        for (int i = 0; i < m_lstHeroInfo.Count; i++)
            m_lstHeroInfo[i].Disable();
    }

    public void SetTeamPosition()
    {
        int i = 0;

        var members = TeamManager.instance.members.Values.
            OrderByDescending(_x => _x.isMain)
            .ThenBy(_x => _x.teamPosition);

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
            m_lstHeroInfo.Find(x => x.key == _hero.info.key)?.UpdateHP();
    }

    public void StopRespawn(CharacterComponent _hero)
    {
        if (_hero.teamPosition > TeamPositionType.NONE)
            m_lstHeroInfo.Find(x => x.key == _hero.info.key).StopRespawn();
    }

    public void StopRespawn()
    {
        for (int i = 0; i < m_lstHeroInfo.Count; i++)
            m_lstHeroInfo[i].StopRespawn();
    }

    public void UseSkill(int _heroIdx)
    {
        var info = m_lstHeroInfo[_heroIdx];
        if (info.isActive == true)
            info.OnButton_UseSkill();
    }

    public int GetIndex(string _key)
        => m_lstHeroInfo.FindIndex(x => x.key == _key);
}
