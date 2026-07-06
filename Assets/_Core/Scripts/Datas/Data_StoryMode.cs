using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class Data_StoryMode
{
    List<StoryModeHistoryData> m_historyData;
    const string c_key = "PP_STORYMODE_HISTORY";

    public string curNodeKey { get; private set; }
    public bool isRunning => curNodeKey.IsActive();

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_historyData = PPWorker.Get<List<StoryModeHistoryData>>(c_key);
        if (m_historyData == null)
            m_historyData = new();
    }

    public async UniTask<bool> EnterAsync(string _nodeKey)
    {
        if (curNodeKey.IsActive())
            return false;

        curNodeKey = _nodeKey;

        await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);

        PopupManager.instance.CloseAll();

        TeamManager.instance.SetState(CharacterStateType.None);
        StageManager.instance.SetState(CharacterStateType.None);

        await UniTask.NextFrame();

        AddressableManager.instance.LoadScene("04_StoryMode");
        return true;
    }

    public async UniTask ExitAsync(int _choiceIdx = -1)
    {
        if (curNodeKey.IsActive() == false)
            return;

        int idx = m_historyData.FindIndex(x => x.key == curNodeKey);
        if (idx == -1)
            m_historyData.Add(new()
            {
                key = curNodeKey,
                choiceIdx = _choiceIdx,
            });
        else
        {
            var data = m_historyData[idx];
            data.choiceIdx = _choiceIdx;
            m_historyData[idx] = data;
        }

        PPWorker.Set(c_key, m_historyData);
        curNodeKey = null;

        await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);

        PopupManager.instance.CloseAll();
        await UniTask.NextFrame();

        AddressableManager.instance.LoadScene("02_Lobby");
    }

    public struct StoryModeHistoryData
    {
        public string key;
        public int choiceIdx;
    }
}
