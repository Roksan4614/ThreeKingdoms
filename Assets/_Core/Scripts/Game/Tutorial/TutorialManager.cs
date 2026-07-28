using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum TutorialType
{
    NONE = -1,

    MOVE,
    ATTACK_NORMAL,
    SKILL_USE,
    DASH_USE,
    STORYMODE_FIRST,

    CASTLE_START,
    CASTLE_WALLY,
    CASTLE_FINISHED,

    MAX
}

public class TutorialManager
{
    public static TutorialManager instance { get; private set; } = new();

    public struct TutorialData
    {
        public int idx;
        public TutorialType curTutorial;
        public List<TutorialType> history;

        public TutorialType maxTutorial => (TutorialType)idx;
    }

    TutorialData m_data;
    public static TutorialData data => instance.m_data;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        //PPWorker.DeleteKey(PlayerPrefsType.GUIDE_QUEST_DATA);
        m_data = PPWorker.Get<TutorialData>(PlayerPrefsType.GUIDE_QUEST_DATA);

        if (m_data.history == null)
        {
            m_data.history = new();
            SaveData();
        }
    }

    public void Complete(TutorialType _type)
    {
        if (m_data.history.Contains(_type) == false)
        {
            m_data.history.Add(_type);
            SaveData();
        }
    }

    public static async UniTask WaitComplete(TutorialType _type, CancellationToken _token)
        => await UniTask.WaitUntil(() => instance.IsComplete(_type), cancellationToken: _token);

    public bool IsComplete(TutorialType _type)
        => m_data.history.Contains(_type);

    void SaveData()
    {
        PPWorker.Set(PlayerPrefsType.GUIDE_QUEST_DATA, m_data);
    }

}
