using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public abstract class StoryModeBaseComponent : MonoBehaviour//, IValidatable
{
    List<StoryModePhaseComponent> m_phases = new();
    protected Queue<TableStringData> m_queTalk = new();

    int m_idxPhase;
    protected int m_resultTalkIdx = -1;
    public int resultTalkIdx => m_resultTalkIdx;

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

        Signal.instance.Update_StoryMode_PlayingMode.connectLambda = new(this, () => PlayingTimerAsync(true).Forget());
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

    protected async UniTask SetNextPhaseAsync()
    {
        Destroy(m_phases[m_idxPhase++].gameObject);
        m_phases[m_idxPhase].gameObject.SetActive(true);

        await UniTask.NextFrame(cancellationToken: m_cts.Token);
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

    protected async UniTask TalkStartAsync(int _count = 1, bool _isActiveMoveCamera = true)
    {
        for (int i = 0; i < _count; i++)
        {
            TableStringData talk = default;
            while (talk.target == null && m_queTalk.Count > 0)
                talk = m_queTalk.Dequeue();

            await TalkStartAsync(talk, _isActiveMoveCamera);
        }
    }

    protected async UniTask TalkStartAsync(TableStringData _talk, bool _isActiveMoveCamera = true)
    {
        if (_talk.target.IsActive() == false)
            return;

        var p = phase;
        if (p.heroes.ContainsKey(_talk.target))
        {
            if (_isActiveMoveCamera == true)
                CameraManager.instance.SetCameraPosTarget(p.heroes[_talk.target].cameraPos, false);

            await p.heroes[_talk.target].talkbox.StartAsyncClickDisable(m_cts.Token, _talk.talkArray);
        }
        else if (p.enemies.ContainsKey(_talk.target))
        {
            if (_isActiveMoveCamera == true)
                CameraManager.instance.SetCameraPosTarget(p.enemies[_talk.target].cameraPos, false);

            await p.enemies[_talk.target].talkbox.StartAsyncClickDisable(m_cts.Token, _talk.talkArray);
        }
    }

    protected void TalkAutoClose(float _duration = 3f, bool _isActiveMoveCamera = true)
        => TalkAutoCloseAsync(_duration, _isActiveMoveCamera).Forget();

    protected async UniTask TalkAutoCloseAsync(float _duration = 3f, bool _isActiveMoveCamera = true)
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

        if (_isActiveMoveCamera == true)
            CameraManager.instance.SetCameraPosTarget(character.cameraPos, false);

        character.talkbox.Start(m_cts.Token, talk.talkArray);

        await UniTask.WaitUntil(() => character.talkbox.isTyping == true);

        if (_duration == 0)
            return;

        var prevText = talk.message;

        var endTime = Time.time + _duration;
        while (endTime > Time.time && ControllerManager.isScreenPointerDown == false)
            await UniTask.NextFrame(cancellationToken: m_cts.Token);

        if (prevText == character.talkbox.text)
            character.talkbox.SetActive(false);

        if (ControllerManager.isScreenPointerDown == true)
            IngameLog.Add("TalkAutoCloseAsync: PointerDown");
    }

    protected async UniTask WaitForSeconds(float _second, bool _isPointerDown = true)
    {
        await UniTask.NextFrame(cancellationToken: m_cts.Token);

        var time = Time.time + _second;

        while (Time.time < time &&
            ((ControllerManager.isScreenPointerDown == false && Input.GetKeyDown(KeyCode.Space) == false) || _isPointerDown == false))
            await UniTask.NextFrame(cancellationToken: m_cts.Token);

        if (ControllerManager.isScreenPointerDown == true)
            IngameLog.Add("WaitForSeconds: PointerDown");
    }

    protected async UniTask WaitPointerDown()
    {
        PlayingTimerAsync(false).Forget();

        await UniTask.WaitUntil(()
            => ControllerManager.isScreenPointerDown || Input.GetKeyDown(KeyCode.Space) || m_isPlayingEnd == true, cancellationToken: m_cts.Token);
    }

    CancellationTokenSource m_cts_PlayingTimer;
    bool m_isPlayingEnd;
    async UniTask PlayingTimerAsync(bool _isForce)
    {
        m_isPlayingEnd = false;

        if (DataManager.storyMode.isPlayingMode == true)
        {
            if (_isForce == false)
            {
                m_cts_PlayingTimer = m_cts_PlayingTimer.ReleaseCTS(true);
                var token = m_cts_PlayingTimer.Token;

                var endTime = Time.time + 1f;
                while (endTime > Time.time)
                    await UniTask.NextFrame(cancellationToken: token);
            }

            m_isPlayingEnd = true;
        }

        m_cts_PlayingTimer = m_cts_PlayingTimer.ReleaseCTS();
    }

    protected CharacterComponent GetHero(string _key)
        => phase.GetHero(_key);
    protected CharacterComponent GetHero(CharacterName _key)
        => phase.GetHero(_key);

    //#region VALIDATE
    //public virtual void OnManualValidate() => m_elementBase.Initialize(transform);

    //[SerializeField, HideInInspector]
    //protected ElementData m_elementBase;

    //[System.Serializable]
    //protected struct ElementData
    //{
    //    public void Initialize(Transform _transform)
    //    {
    //    }
    //}
    //#endregion VALIDATE

}
