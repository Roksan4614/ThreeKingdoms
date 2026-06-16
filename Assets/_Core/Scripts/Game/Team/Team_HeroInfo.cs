using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Team_HeroInfo
{
    List<HeroInfoComponent> m_lstHeroInfo = new();

    bool m_isHide;

    RectTransform m_panel;
    Transform m_arrowHide;

    float m_prevPosY;

    public Team_HeroInfo(Transform _heroInfo)
    {
        if (_heroInfo == null)
            return;

        m_panel = (RectTransform)_heroInfo.Find("Panel");
        m_prevPosY = m_panel.anchoredPosition.y;

        var layout = m_panel.Find("Layout");

        for (int i = 0; i < layout.childCount; i++)
            m_lstHeroInfo.Add(layout.GetChild(i).GetComponent<HeroInfoComponent>());

        _heroInfo.GetComponent<Button>("btn_hide").onClick.AddListener(() => OnButtonAsync_Hide(true).Forget());
        m_arrowHide = _heroInfo.Find("btn_hide/Text");

        m_isHide = true;
        OnButtonAsync_Hide(false).Forget();
    }

    async UniTask OnButtonAsync_Hide(bool _isTween)
    {
        m_isHide = !m_isHide;

        if (m_isHide == false)
            m_panel.gameObject.SetActive(true);

        var scaleArrow = m_arrowHide.localScale;
        scaleArrow.y = m_isHide ? 1 : -1;
        m_arrowHide.localScale = scaleArrow;

        var duration = 0.05f;
        ControllerManager.instance.SetMoveActionArea(m_isHide, true, duration);

        if (BossRaidWorker.instance.isRunning == true)
            RankBossRaidComponent.instance.SetMoveArea(m_isHide, true, duration);
        else
            MissionComponent.instance.SetMoveArea(m_isHide, true, duration);

        var pos = m_panel.anchoredPosition;
        pos.y = m_isHide ? m_prevPosY - m_panel.rect.height : m_prevPosY;

        if (_isTween == true)
        {
            await m_panel.DOAnchorPosY(pos.y, duration)//.SetEase(m_isHide ? Ease.InBack : Ease.OutBack)
                .AsyncWaitForCompletion();
        }
        else
            m_panel.anchoredPosition = pos;

        if (m_isHide == true)
            m_panel.gameObject.SetActive(false);
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
