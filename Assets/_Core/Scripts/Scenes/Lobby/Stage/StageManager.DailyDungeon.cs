using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class StageManager
{
    AsyncOperationHandle<GameObject> m_handleDailyDungeon;

    public async UniTask InitializeAsync_DailyDungeon()
    {
        await LoadDailyDungeonAsync();
    }

    async UniTask LoadDailyDungeonAsync()
    {
        string key = $"DailyDungeon/DailyDungeon_{DataManager.dailyDungeon.enterWeekday}.prefab";

        await AddressableManager.instance.LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                m_handleDailyDungeon = data.Value;
            }
        }, null, key);

        var slot = Instantiate(m_handleDailyDungeon.Result, m_element.chapter);//.GetComponent<BossRaid_BossSlotComponent>();
    }

    void OnDestroy_DailyDungeon()
    {
        if (m_handleDailyDungeon.IsValid())
            m_handleDailyDungeon.Release();
    }
}
