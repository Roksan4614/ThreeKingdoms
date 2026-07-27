using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class StageManager
{
    AsyncOperationHandle<GameObject> m_handleStoryMode;

    public StoryModeBaseComponent slotStory { get; private set; }

    public async UniTask InitializeAsync_StoryMode()
    {
        await LoadStoryModeAsync(DataManager.storyMode.curNodeKey);
    }

    async UniTask LoadStoryModeAsync(string _nodeKey)
    {
        string key = $"Story/Story_{_nodeKey}.prefab";

        await AddressableManager.instance.LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                m_handleDailyDungeon = data.Value;
            }
        }, null, key);

        if (m_handleDailyDungeon.IsValid() == false)
        {
            await LoadStoryModeAsync("none");
            return;
        }
        slotStory = Instantiate(m_handleDailyDungeon.Result, m_element.chapter).GetComponent<StoryModeBaseComponent>();
    }

    void OnDestroy_StoryMode()
    {
        if (m_handleStoryMode.IsValid())
            m_handleStoryMode.Release();
    }
}
