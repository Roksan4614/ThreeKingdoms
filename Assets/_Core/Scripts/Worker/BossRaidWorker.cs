using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.EventSystems.EventTrigger;

public class BossRaidWorker : MonoSingleton<BossRaidWorker>
{
    public enum BossRaidType
    {
        NONE = -1,

        LuBu,   // 여포
    }

    bool m_isDoing = false;
    AsyncOperationHandle<GameObject> m_handle;

    public async UniTask Initialize(BossRaidType _bossType)
    {
        if (m_isDoing == false)
            m_isDoing = true;

        await PopupManager.instance.ShowDimmAsync(true);

        bool isStart = await StartAsync(_bossType);

        await PopupManager.instance.ShowDimmAsync(false, _duration: isStart ? 1f : .5f);

        m_isDoing = false;
    }

    async UniTask<bool> StartAsync(BossRaidType _bossType)
    {
        string key = $"BossRaid/BossRaid_{_bossType}.prefab";

        if (await ConnectAsync() == false)
            return false;

        await AddressableManager.instance.LoadAssetAsync<GameObject>(
            _result =>
            {
                if (_result.Count > 0)
                    m_handle = _result.First().Value;
            }, null, key);

        if (m_handle.IsValid() == false)
            return false;

        var bossRaid = Instantiate(m_handle.Result, StageManager.instance.transform).transform;
        var boss = bossRaid.Find("Boss").GetChild(0).GetComponent<Character_Enemy>();

        if (boss != null)
        {
            // 보스 스탯 넣어줘야 해
            boss.SetBossData("");

            StageManager.instance.RestartStage(false);
            MapManager.instance.FadeDimm(false, 0);

            InfoStageComponent.instance.SetBossRaid(true);

            await PopupManager.instance.ShowDimmAsync(false);

            boss.move.MoveTarget(TeamManager.instance.GetNearestHero(boss.transform.position), true);
            TeamManager.instance.MoveAttactTarget(boss);

            await UniTask.WaitUntil(() => boss.isLive == false);

            await OpenResultAsync();

            await UniTask.WaitForSeconds(100);

            await PopupManager.instance.ShowDimmAsync(true, _duration: 1f);

            Destroy(bossRaid.gameObject);
            m_handle.Release();
            InfoStageComponent.instance.SetBossRaid(false);

            return true;
        }
        else
        {
            Destroy(bossRaid.gameObject);
            m_handle.Release();

            return false;
        }
    }

    async UniTask<bool> ConnectAsync()
    {
        await UniTask.Yield();

        return true;
    }

    async UniTask OpenResultAsync()
    {
        await UniTask.Yield();
    }

    protected override void OnDestroy()
    {
        if (m_handle.IsValid())
            m_handle.Release();

        base.OnDestroy();
    }
}
