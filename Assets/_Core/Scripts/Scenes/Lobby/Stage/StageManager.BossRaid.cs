using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class StageManager
{
    AsyncOperationHandle<GameObject> m_handleBoss;

    public async UniTask InitializeAsync_BossRaid()
    {
        await LoadBossRaid();
    }

    async UniTask LoadBossRaid()
    {
        string key = $"BossRaid/BossRaid_{BossRaidWorker.instance.bossType}.prefab";

        await AddressableManager.instance.LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                m_handleBoss = data.Value;
            }
        }, null, key);

        var slot = Instantiate(m_handleBoss.Result, m_element.chapter).GetComponent<BossRaid_BossSlotComponent>();
    }

    void FinishedBossRaid()
    {
        m_handleBoss.Release();
    }
}
