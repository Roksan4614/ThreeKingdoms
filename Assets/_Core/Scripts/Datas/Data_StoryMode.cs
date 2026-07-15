using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class Data_StoryMode
{
    List<StoryModeHistoryData> m_historyData;
    const string c_key = "PP_STORYMODE_HISTORY";

    public StoryModeHistoryData lastHistory { get; set; }
    public string curNodeKey { get; private set; }
    public bool isRunning => curNodeKey.IsActive();
    public int historyCount => m_historyData.Count;
    public int siblingIndexSlot { get; private set; }
    public int siblingIndexNode { get; private set; }

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_historyData = PPWorker.Get<List<StoryModeHistoryData>>(c_key);
        if (m_historyData == null)
            m_historyData = new();
    }

    public async UniTask EnterAsync(string _nodeKey)
    {
        if (curNodeKey.IsActive())
            return;

        curNodeKey = _nodeKey;

        await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);

        PopupManager.instance.CloseAll();

        TeamManager.instance.SetState(CharacterStateType.None);
        StageManager.instance.SetState(CharacterStateType.None);

        await UniTask.NextFrame();

        AddressableManager.instance.LoadScene("04_StoryMode");
    }

    public string nodeKeyNewClear { get; set; }
    public bool isExit { get; set; }
    public async UniTask ExitAsync(int _choiceIdx = -1)
    {
        if (curNodeKey.IsActive() == false)
            return;
        SaveHistoryData(curNodeKey, _choiceIdx);
        lastHistory = m_historyData.Find(x => x.key == curNodeKey);

        m_nextPlayOrderNumber = 0;
        curNodeKey = null;

        List<UniTask> tasks = new();
        tasks.Add(PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f));

        var storyNode = TableManager.storyNode.GetNode(curNodeKey);
        if (storyNode.reward_character.IsActive() == true)
        {
            tasks.Add(AddressableManager.instance.Load_HeroIconAsync(storyNode.reward_character));
            tasks.Add(AddressableManager.instance.Load_HeroCharacterAsync(storyNode.reward_character));
        }

        await UniTask.WhenAll(tasks);

        PopupManager.instance.CloseAll();
        await UniTask.NextFrame();

        isExit = true;
        AddressableManager.instance.LoadScene("02_Lobby");
    }

    public void SaveHistoryData(string _nodeKey, int _choiceIdx)
    {
        int idx = m_historyData.FindIndex(x => x.key == _nodeKey);
        if (idx == -1)
        {
            nodeKeyNewClear = _nodeKey;
            m_historyData.Add(new()
            {
                key = _nodeKey,
                choiceIdx = _choiceIdx,
            });

            m_historyData = m_historyData.SortBy(x => TableManager.storyNode.GetNode(x.key).order_num);
        }
        else
        {
            var data = m_historyData[idx];
            data.choiceIdx = _choiceIdx;
            m_historyData[idx] = data;
        }
        PPWorker.Set(c_key, m_historyData);
    }

    public void ResetIFMode(string _nodeKey)
    {
        var nodeData = TableManager.storyNode.GetNode(_nodeKey);

        SaveHistoryData(_nodeKey, nodeData.hasIfStory == 2 ? 1 : 2);

        while (nodeData.next_node_key.IsActive())
        {
            nodeData = TableManager.storyNode.GetNode(nodeData.next_node_key);

            var idx = m_historyData.FindIndex(x => x.key == nodeData.node_key);
            if (idx > -1)
                m_historyData.RemoveAt(idx);
        }

        PPWorker.Set(c_key, m_historyData);

        m_nextPlayOrderNumber = 0;
        Signal.instance.UnlockStoryMode.Emit();
    }

    //public void TestSave(Table_StoryMode_Node.TableStoryModeNodeData _storyNode)
    //{
    //    SaveHistoryData(_storyNode.node_key, _storyNode.hasIfStory > 0 ? 2 : 1);
    //    lastHistory = m_historyData.Find(x => x.key == curNodeKey);

    //    m_nextPlayOrderNumber = 0;

    //    Signal.instance.UnlockStoryMode.Emit();
    //}

    int m_nextPlayOrderNumber;
    public int nextPlayOrderNumber
    {
        get
        {
            if (m_nextPlayOrderNumber == 0)
            {
                if (m_historyData.Count == 0)
                {
                    var dbStory = TableManager.storyNode.list.ToList().FindAll(x => x.chapter_key > 0);

                    m_nextPlayOrderNumber = dbStory[0].order_num;
                }
                else if (StageManager.instance.data.level == 1)
                {
                    var prev = TableManager.storyNode.GetNode(m_historyData[m_historyData.Count - 1].key);
                    var next = TableManager.storyNode.GetNode_Next(prev);

                    //조건없는 노드가 있을때까지 찾기
                    while (next != null && next.FindAll(x => x.isConditional == false).Count == 0)
                    {
                        next = TableManager.storyNode.GetNode_Next(next[0]);
                    }

                    if (next != null && next.Count > 0)
                        m_nextPlayOrderNumber = next[0].order_num;
                }
                else
                    m_nextPlayOrderNumber = int.MaxValue;
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
                if (stageData.level > 1)
                {
                    m_nextOpenOrderNumber = int.MaxValue;
                    return m_nextOpenOrderNumber;
                }

                var dbStory = TableManager.storyNode.list.ToList()
                    .FindAll(x =>
                        (x.chapter_key > stageData.chapterNumber ||
                        (x.stage_key >= stageData.stageNumber && x.chapter_key == stageData.chapterNumber))
                        && x.chapter_key > 0);

                if (dbStory.Count > 0)
                    m_nextOpenOrderNumber = dbStory[0].order_num;
            }
            return m_nextOpenOrderNumber;
        }
    }
    public void ClearStage_AddStoryMode(StageManager.LoadData_Stage _stageInfo)
    {
        var dbStory = TableManager.storyNode.list.ToList()
            .FindAll(x =>
                (x.chapter_key > _stageInfo.chapterNumber ||
                (x.stage_key >= _stageInfo.stageNumber && x.chapter_key == _stageInfo.chapterNumber))
                && x.chapter_key > 0);

        if (dbStory.Count == 0 || _stageInfo.level > 1)
        {
            m_nextOpenOrderNumber = int.MaxValue;
            return;
        }

        var nextStory = dbStory[0];

        if (nextStory.chapter_key == _stageInfo.chapterNumber && nextStory.stage_key == _stageInfo.stageNumber)
        {
            for (int i = 0; i < dbStory.Count; i++)
            {
                bool isNextSame = i + 1 < dbStory.Count && dbStory[i + 1].order_num == dbStory[i].order_num;

                if (isNextSame == false)
                {
                    m_nextOpenOrderNumber = dbStory[i + 1].order_num;
                    break;
                }
            }

            PopupManager.instance.AlertShow("해금된_스토리가_있습니다.");
            Signal.instance.UnlockStoryMode.Emit();
        }
    }

    public bool IsUnlock(string _nodeKey)
    {
        var unlockData = TableManager.storyUnlock.GetSourceNodeKey(_nodeKey);

        for (int i = 0; i < unlockData.Length; i++)
        {
            if (m_historyData.FindIndex(x => x.key == unlockData[i].source_node_key && x.choiceIdx == unlockData[i].required_choice_seq) == -1)
                return false;
        }

        return true;
    }

    public string GetChoiceSeq(string _nodeKey, bool _isDesc)
    {
        var historyData = m_historyData.Find(x => x.key == _nodeKey);

        if (historyData.choiceIdx == 0)
            return null;

        var nodeData = TableManager.storyNode.GetNode(_nodeKey);

        string key = $"{nodeData.node_key.ToUpper()}_CHOICE_{historyData.choiceIdx}";
        if (_isDesc)
            key += "_DESC";

        if (TableManager.storyString.Exists(key) == false)
            return null;

        return $"선택_{historyData.choiceIdx}: {TableManager.storyString.GetString(key)}";
    }

    public void SetPopupSiblingIndex(int _siblingSlot, int _siblingNode)
    {
        siblingIndexSlot = _siblingSlot;
        siblingIndexNode = _siblingNode;
    }

    public bool IsComplete(string _nodeKey)
        => m_historyData.FindIndex(x => x.key == _nodeKey) > -1;

    public Table_StoryMode_Node.TableStoryModeNodeData GetOpenIFMode()
    {
        for (int i = 0; i < m_historyData.Count; i++)
        {
            var nodeData = TableManager.storyNode.GetNode(m_historyData[i].key);

            if (nodeData.hasIfStory == m_historyData[i].choiceIdx)
                return nodeData;
        }
        return default;
    }

    public struct StoryModeHistoryData
    {
        public string key;
        public int choiceIdx;

        public bool isActive => key.IsActive();
    }
}
