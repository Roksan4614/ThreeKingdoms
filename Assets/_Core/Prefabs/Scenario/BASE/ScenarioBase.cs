using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class ScenarioBase : MonoBehaviour, IValidatable
{
    protected bool isTimelineFinished = false;

    protected virtual void Start()
    {
        if (TeamManager.instance != null)
            transform.position = TeamManager.instance.mainHero.transform.position;
    }

    protected List<TableStringData> m_dbTalk;

    public async UniTask InitializeAsync(string _stageKey)
    {
        m_dbTalk = TableManager.scenarioTalk.GetTalk(_stageKey, true).ToList();

        await StartAsync();
    }

    protected virtual async UniTask StartAsync()
    {
        await PopupManager.instance.ShowDimmAsync(false);

        await UniTask.WaitUntil(() => isTimelineFinished == true);

        await PopupManager.instance.ShowDimmAsync(true);
    }

    public void Playable_TalkStart(CharacterComponent _hero, string _message)
        => _hero.talkbox.Start(_message);

    public async UniTask Playable_TalkEndAsync(CharacterComponent _hero)
    {
        if (_hero.talkbox.isTyping == true)
        {
            m_elementBase.playableDirector.Pause();
            await UniTask.WaitUntil(() => _hero.talkbox.isTyping == false);
            m_elementBase.playableDirector.Play();
        }
    }

    public void Playable_TimelineFinished()
    {
        isTimelineFinished = true;
    }

    protected CharacterComponent GetHeroComp(TableStringData _tableData)
    {
        var hero = _tableData.target.IsActive() ? TeamManager.instance.GetHero(_tableData.target) : TeamManager.instance.mainHero;

        return hero;
    }

    protected async UniTask StartTalkAsync(TableStringData _tableData)
    {
        var hero = _tableData.target.IsActive() ? TeamManager.instance.GetHero(_tableData.target) : TeamManager.instance.mainHero;

        await hero.talkbox.StartAsyncClickDisable(_tableData.talkArray);
    }

    public virtual void OnManualValidate()
    {
        m_elementBase.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    protected ElementBaseData m_elementBase;

    [Serializable]
    protected struct ElementBaseData
    {
        public Character_Enemy[] enemy;
        public CharacterComponent[] hero;

        public PlayableDirector playableDirector;

        public void Initialize(Transform _transform)
        {
            enemy = _transform.Find("Enemy").GetComponentsInChildren<Character_Enemy>();
            hero = _transform.Find("Hero").GetComponentsInChildren<CharacterComponent>();

            playableDirector = _transform.GetComponent<PlayableDirector>();
        }
    }
}
