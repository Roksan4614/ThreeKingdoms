using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class StageManager
{
    AsyncOperationHandle<GameObject> m_handleStoryMode;

    public async UniTask InitializeAsync_StoryMode()
    {
        await LoadStoryModeAsync();
    }

    async UniTask LoadStoryModeAsync()
    {
        string key = $"Story/Story_{DataManager.storyMode.curNodeKey}.prefab";

        await AddressableManager.instance.LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                m_handleDailyDungeon = data.Value;
            }
        }, null, key);

        var slot = Instantiate(m_handleDailyDungeon.Result, m_element.chapter);//.GetComponent<BossRaid_BossSlotComponent>();
    }

    void OnDestroy_StoryMode()
    {
        if (m_handleStoryMode.IsValid())
            m_handleStoryMode.Release();
    }
}
