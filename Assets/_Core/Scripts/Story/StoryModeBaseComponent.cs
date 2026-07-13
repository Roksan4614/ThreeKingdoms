using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public abstract class StoryModeBaseComponent : MonoBehaviour, IValidatable
{
    List<StoryModePhaseComponent> m_phases = new();
    protected Queue<TableStringData> m_queTalk = new();

    int m_idxPhase;
    protected int m_resultTalkIdx = -1;

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

        ControllerManager.instance.SetActiveButton_StoryMode(false);
        m_cts = m_cts.ReleaseCTS(true);
        await StartAsync();

        DataManager.storyMode.ExitAsync(m_resultTalkIdx).Forget();
    }

    protected abstract UniTask StartAsync();

    protected void SetNextPhase()
    {
        m_phases[m_idxPhase].gameObject.SetActive(false);
        m_idxPhase++;
        m_phases[m_idxPhase].gameObject.SetActive(true);
    }


    protected StoryModePhaseComponent phase => m_idxPhase < m_phases.Count ? m_phases[m_idxPhase] : null;

    protected CharacterComponent mainHero => phase?.mainHero;

    //이어지는 대사 가져오기
    Queue<TableStringData> NextTalkStringTableArray()
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

    string[] NextTalkArray()
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
                var peekTarget = m_queTalk.Peek().target;

                if (peekTarget == target)
                {
                    result.Add(m_queTalk.Dequeue().message);
                    continue;
                }
                else if (peekTarget == null)
                    m_queTalk.Dequeue();

                break;
            }
        }

        return result.ToArray();
    }

    protected bool IsTalkEnd() => m_queTalk.Peek().target.IsActive();

    protected async UniTask TalkStartAsync()
    {
        TableStringData talk = default;
        while (talk.target == null && m_queTalk.Count > 0)
            talk = m_queTalk.Dequeue();

        if (talk.target.IsActive() == false)
            return;

        var p = phase;
        if (p.heroes.ContainsKey(talk.target))
        {
            CameraManager.instance.SetCameraPosTarget(p.heroes[talk.target].element.cameraPos, false);
            await p.heroes[talk.target].talkbox.StartAsyncClickDisable(m_cts.Token, talk.talkArray);
        }
        else if (p.enemies.ContainsKey(talk.target))
        {
            CameraManager.instance.SetCameraPosTarget(p.enemies[talk.target].element.cameraPos, false);
            await p.enemies[talk.target].talkbox.StartAsyncClickDisable(m_cts.Token, talk.talkArray);
        }
    }

    protected void TalkAutoClose(float _duration = 3f)
        => TalkAutoCloseAsync(_duration).Forget();

    protected async UniTask TalkAutoCloseAsync(float _duration = 3f)
    {
        TableStringData talk = default;
        while (talk.target == null && m_queTalk.Count > 0)
            talk = m_queTalk.Dequeue();

        if (talk.target.IsActive() == false)
            return;

        var p = phase;

        var character = p.heroes.ContainsKey(talk.target) ? p.heroes[talk.target] :
            p.enemies.ContainsKey(talk.target) ? p.enemies[talk.target] : null;

        if (character == null)
            return;

        CameraManager.instance.SetCameraPosTarget(character.element.cameraPos, false);
        character.talkbox.Start(m_cts.Token, talk.talkArray);

        var prevText = talk.message;
        await UniTask.WaitForSeconds(_duration, cancellationToken: m_cts.Token);

        if (prevText == character.talkbox.text)
            character.talkbox.SetActive(false);
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
