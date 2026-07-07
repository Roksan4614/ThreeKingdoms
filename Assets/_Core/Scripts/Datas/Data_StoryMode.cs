using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
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
        {
            m_historyData.Add(new()
            {
                key = curNodeKey,
                choiceIdx = _choiceIdx,
            });

            m_nextPlayOrderNumber = 0;
            m_historyData = m_historyData.SortBy(x => TableManager.storyNode.GetNode(x.key).order_num);
        }
        else
        {
            var data = m_historyData[idx];
            data.choiceIdx = _choiceIdx;
            m_historyData[idx] = data;


            PPWorker.Set(c_key, m_historyData);
            curNodeKey = null;

            await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);

            PopupManager.instance.CloseAll();
            await UniTask.NextFrame();

            AddressableManager.instance.LoadScene("02_Lobby");
        }
    }

    int m_nextPlayOrderNumber;
    public int nextPlayOrderNumber
    {
        get
        {
            if (m_nextPlayOrderNumber == 0)
            {
                if (m_historyData.Count == 0)
                {
                    m_nextPlayOrderNumber = 1;
                }
                else
                {
                    var next = TableManager.storyNode.GetNode_OrderNum(m_historyData[m_historyData.Count - 1].orderNum + 1);

                    //조건없는 노드가 있을때까지 찾기
                    while (next.Count > 0 && next.FindAll(x => x.isConditional == false).Count == 0)
                        next = TableManager.storyNode.GetNode_OrderNum(next[0].order_num + 1);

                    if (next.Count > 0)
                        m_nextPlayOrderNumber = next[0].order_num;
                }
            }
            return m_nextPlayOrderNumber;
        }
    }

    int m_nextOpenOrderNumber;
    public int nextOpenOrderNumber
    {
        get
        {
            if (m_nextOpenOrderNumber == 0)
            {
                var stageData = StageManager.instance.data;

                var dbStory = TableManager.storyNode.list.SortBy(x => x.order_num)
                    .FindAll(x =>
                        x.chapter_key > stageData.chapterNumber ||
                        (x.stage_key >= stageData.stageNumber && x.chapter_key == stageData.chapterNumber));

                m_nextOpenOrderNumber = dbStory[0].order_num;
            }
            return m_nextOpenOrderNumber;
        }
    }
    public void CheckStoryMode(StageManager.LoadData_Stage _stageInfo)
    {
        var dbStory = TableManager.storyNode.list.SortBy(x => x.order_num)
            .FindAll(x =>
                x.chapter_key > _stageInfo.chapterNumber ||
                (x.stage_key >= _stageInfo.stageNumber && x.chapter_key == _stageInfo.chapterNumber));

        if (dbStory.Count == 0)
        {
            m_nextOpenOrderNumber = int.MaxValue;
            return;
        }

        var nextStory = dbStory[0];

        if (nextStory.chapter_key == _stageInfo.chapterNumber && nextStory.stage_key == _stageInfo.stageNumber)
        {
            PopupManager.instance.AlertShow("해금된_스토리가_있습니다.");
            BannerComponent.instance.RedDot_StoryMode();
        }

        if (dbStory.Count > 1)
            m_nextOpenOrderNumber = dbStory[1].order_num;
    }

    public struct StoryModeHistoryData
    {
        public string key;
        public int choiceIdx;
        public int orderNum;
    }
}
