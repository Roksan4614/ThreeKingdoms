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

    public int siblingIndexSlot { get; private set; }
    public int siblingIndexNode { get; private set; }

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

    public bool isExit { get; set; }
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
        }

        PPWorker.Set(c_key, m_historyData);
        curNodeKey = null;

        await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);

        PopupManager.instance.CloseAll();
        await UniTask.NextFrame();

        isExit = true;
        AddressableManager.instance.LoadScene("02_Lobby");
    }

    public void TestSave(Table_StoryMode_Node.TableStoryModeNodeData _storyNode)
    {
        int idx = m_historyData.FindIndex(x => x.key == _storyNode.node_key);
        if (idx == -1)
        {
            m_historyData.Add(new()
            {
                key = _storyNode.node_key,
                choiceIdx = 1,
            });

            m_historyData = m_historyData.SortBy(x => TableManager.storyNode.GetNode(x.key).order_num);
        }
        else
        {
            var data = m_historyData[idx];
            data.choiceIdx = 1;
            m_historyData[idx] = data;
        }

        m_nextPlayOrderNumber = 0;
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
                    var prev = TableManager.storyNode.GetNode(m_historyData[m_historyData.Count - 1].key);
                    var next = TableManager.storyNode.GetNode_OrderNum(prev.order_num + 1);

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
    public void ClearStage_AddStoryMode(StageManager.LoadData_Stage _stageInfo)
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

        for (int i = 0; i < dbStory.Count; i++)
        {
            bool isNextSame = i + 1 < dbStory.Count && dbStory[i + 1].order_num == dbStory[i].order_num;

            if (isNextSame == false)
            {
                m_nextOpenOrderNumber = dbStory[i + 1].order_num;
                break;
            }
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

    public struct StoryModeHistoryData
    {
        public string key;
        public int choiceIdx;
    }
}
