using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public abstract class StoryModeBaseComponent : MonoBehaviour, IValidatable
{
    List<StoryModePhaseComponent> m_phases = new();
    Queue<TableStringData> m_queTalk = new();

    int m_idxPhase;

    protected CancellationTokenSource m_cts;

    protected virtual void Start()
    {
        ControllerManager.instance.SetSwitch(false);

        TeamManager.instance.SetHeroInfoHide(true, false);
        HeroNavigationComponent.instance.gameObject.SetActive(false);

        var phase = transform.Find("Phase");
        for (int i = 0; i < phase.childCount; i++)
        {
            m_phases.Add(phase.GetChild(i).GetComponent<StoryModePhaseComponent>());
            m_phases[i].gameObject.SetActive(i == 0);
        }

        m_queTalk = TableManager.scenarioTalk.GetTalk(DataManager.storyMode.curNodeKey.ToUpper(), true);

        WaitReadyAsync().Forget();
    }

    void OnDestroy()
    {
        if (m_isPrevAutoSkill == true)
        {
            DataManager.option.SetSkipSave();
            DataManager.option.isAutoSkill = true;
        }

        m_cts = m_cts.ReleaseCTS();
    }

    bool m_isPrevAutoSkill;
    async UniTask WaitReadyAsync()
    {
        m_isPrevAutoSkill = DataManager.option.isAutoSkill;

        if (m_isPrevAutoSkill == true)
        {
            DataManager.option.SetSkipSave();
            DataManager.option.isAutoSkill = false;
        }

        await UniTask.WaitUntil(() => Scene_StoryMode.instance.isReady == true);

        m_cts = m_cts.ReleaseCTS(true);
        await StartAsync();
    }

    protected abstract UniTask StartAsync();

    protected async UniTask SetNextPhaseAsync()
    {
        await PopupManager.instance.ShowDimmAsync(true);

        m_phases[m_idxPhase].gameObject.SetActive(false);
        m_idxPhase++;
        m_phases[m_idxPhase].gameObject.SetActive(true);

        PopupManager.instance.ShowDimm(false);
    }

    protected StoryModePhaseComponent phase => m_idxPhase < m_phases.Count ? m_phases[m_idxPhase] : null;

    protected CharacterComponent mainHero => phase?.mainHero;

    //이어지는 대사 가져오기
    protected Queue< TableStringData> NextTalkStringTableArray()
    {
        Queue<TableStringData> result = new();
        if (m_queTalk.Count > 0)
        {
            while (m_queTalk.Count > 0)
            {
                var talk = m_queTalk.Dequeue();
                if (talk.target.IsActive() == true)
                {
                    result.Enqueue(talk);
                    break;
                }
            }

            var target = result.Peek().target;
            while (m_queTalk.Count > 0)
            {
                if (m_queTalk.Peek().target == target)
                    result.Enqueue(m_queTalk.Dequeue());
                else
                    break;
            }
        }

        return result;
    }

    protected string[] NextTalkArray()
    {
        List<string> result = new();
        if (m_queTalk.Count > 0)
        {
            string target = null;
            while (m_queTalk.Count > 0)
            {
                var talk = m_queTalk.Dequeue();
                if (talk.target.IsActive() == true)
                {
                    target = talk.target;
                    result.Add(talk.message);
                    break;
                }
            }

            while (m_queTalk.Count > 0)
            {
                if (m_queTalk.Peek().target == target)
                    result.Add(m_queTalk.Dequeue().message);
                else
                    break;
            }
        }

        return result.ToArray();
    }

    protected bool IsTalkEnd() => m_queTalk.Peek().target.IsActive();

    protected async UniTask TalkStartAsync()
    {
        if (m_queTalk.Count == 0)
            return;

        var talks = m_queTalk.Dequeue();

        var p = phase;
        if (p.heroes.ContainsKey(talks.target))
        {
            CameraManager.instance.SetCameraPosTarget(p.heroes[talks.target].element.cameraPos);
            await p.heroes[talks.target].talkbox.StartAsyncClickDisable(m_cts.Token, talks.talkArray);
        }
        else if (p.enemies.ContainsKey(talks.target))
        {
            CameraManager.instance.SetCameraPosTarget(p.enemies[talks.target].element.cameraPos);
            await p.enemies[talks.target].talkbox.StartAsyncClickDisable(m_cts.Token, talks.talkArray);
        }
    }

    protected void TalkAutoClose(float _duration = 3f)
        => TalkkAutoCloseAsync(_duration).Forget();

    protected async UniTask TalkkAutoCloseAsync(float _duration = 3f)
    {
        var talks = m_queTalk.Dequeue();

        var p = phase;

        var character = p.heroes.ContainsKey(talks.target) ? p.heroes[talks.target] :
            p.enemies.ContainsKey(talks.target) ? p.enemies[talks.target] : null;

        if (character == null)
            return;

        CameraManager.instance.SetCameraPosTarget(character.element.cameraPos);
        character.talkbox.Start(m_cts.Token, talks.talkArray);

        await UniTask.WaitForSeconds(_duration, cancellationToken:m_cts.Token);

        character.talkbox.SetActive(false);
    }

    protected async UniTask TalkStartGroupAsync()
    {
        var talks = NextTalkStringTableArray();

        var p = phase;
        foreach (var t in talks)
        {
            if (p.heroes.ContainsKey(t.target))
            {
                CameraManager.instance.SetCameraPosTarget(p.heroes[t.target].element.cameraPos);
                await p.heroes[t.target].talkbox.StartAsyncClickDisable(m_cts.Token, t.talkArray);
            }
            else if (p.enemies.ContainsKey(t.target))
            {
                CameraManager.instance.SetCameraPosTarget(p.enemies[t.target].element.cameraPos);
                await p.enemies[t.target].talkbox.StartAsyncClickDisable(m_cts.Token, t.talkArray);
            }
        }
    }

    #region VALIDATE
    public virtual void OnManualValidate() => m_elementBase.Initialize(transform);

    [SerializeField, HideInInspector]
    protected ElementData m_elementBase;

    [System.Serializable]
    protected struct ElementData
    {
        public void Initialize(Transform _transform)
        {
        }
    }
    #endregion VALIDATE

}
