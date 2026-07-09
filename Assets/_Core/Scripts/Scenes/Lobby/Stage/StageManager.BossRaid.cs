using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class StageManager
{
    AsyncOperationHandle<GameObject> m_handleBossRaid;

    public async UniTask InitializeAsync_BossRaid()
    {
        await LoadBossRaidAsync();
    }

    async UniTask LoadBossRaidAsync()
    {
        string key = $"BossRaid/BossRaid_{BossRaidWorker.instance.bossType}.prefab";

        await AddressableManager.instance.LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                m_handleBossRaid = data.Value;
            }
        }, null, key);

        if (m_handleBossRaid.IsValid() == false)
            return;

        var slot = Instantiate(m_handleBossRaid.Result, m_element.chapter).GetComponent<BossRaid_BossSlotComponent>();
    }

    void OnDestroy_BossRaid()
    {
        if (m_handleBossRaid.IsValid())
            m_handleBossRaid.Release();
    }
}
