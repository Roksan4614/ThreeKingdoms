using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cysharp.Threading.Tasks;
using ThreeKingdoms.Client.Server;
using ThreeKingdoms.Shared.Enums;
using ThreeKingdoms.Shared.Rest;
using ThreeKingdoms.Shared.Types;
using UnityEngine;
using UnityEngine.Events;

public partial class Data_DailyDungeon
{
    private DailyDungeonLobbyDto m_serverLobby;
    private DailyDungeonEntryDto m_serverEntry;
    private DailyDungeonFinishReq m_serverFinish;
    private RestRequestOptions m_serverFinishOptions;
    private RestRequestOptions m_pendingSweepOptions;
    private long m_pendingSweepBoss;
    private RestRequestOptions m_pendingAdOptions;
    private bool m_serverBusy;
    private bool m_finishing;
    private long m_serverBossHp = 1;
    private long m_serverBossMaxHp = 1;

    public WeekdayType CurrentWeekday => GameServer.Enabled && m_serverLobby != null
        ? (WeekdayType)m_serverLobby.Weekday : (WeekdayType)Utils.GetUTC().DayOfWeek;
    public float PlayTimeSeconds => GameServer.Enabled && m_serverEntry != null ? m_serverEntry.PlayTimeSeconds : 60f;

    public async UniTask RefreshServerAsync()
    {
        if (!GameServer.Enabled) return;
        var response = await GameServer.DailyDungeon.LobbyAsync(new DailyDungeonLobbyReq(), GameServer.Options());
        ApplyServerLobby(response.Data ?? throw new InvalidOperationException("Daily dungeon response is empty."));
    }

    private void ApplyServerLobby(DailyDungeonLobbyDto snapshot)
    {
        if (m_serverLobby != null && long.TryParse(snapshot.Revision, out var incoming)
            && long.TryParse(m_serverLobby.Revision, out var previous) && incoming < previous) return;
        m_serverLobby = snapshot;
        m_data ??= new DailyDungeonData();
        m_data.count = checked((int)snapshot.RemainingCount);
        m_data.adCount = checked((int)Math.Max(0, snapshot.AdLimit - snapshot.AdUsedCount));
        m_recordData = snapshot.Records.Select(record => new DailyDungeonRecordData
        {
            weekday = BossWeekday(record.BossId),
            gradeType = record.HighestKilledGrade.HasValue ? (GradeType)(int)record.HighestKilledGrade.Value : GradeType.NONE,
            percent = 0
        }).ToList();
    }

    private static long BossId(WeekdayType weekday)
    {
        var row = GameServer.TableRows("s_daily_dungeon_boss").FirstOrDefault(x => (int)x["weekday"] == (int)weekday);
        return row == null ? throw new InvalidOperationException("Daily dungeon boss is missing: " + weekday) : (long)row["idx"];
    }

    private static WeekdayType BossWeekday(long bossId)
    {
        var row = GameServer.TableRows("s_daily_dungeon_boss").FirstOrDefault(x => (long)x["idx"] == bossId);
        return row == null ? throw new InvalidOperationException("Daily dungeon boss is missing: " + bossId) : (WeekdayType)(int)row["weekday"];
    }

    private async UniTask<bool> EnterServerAsync(WeekdayType weekday)
    {
        if (m_serverBusy) return false;
        m_serverBusy = true;
        try
        {
            var response = await GameServer.DailyDungeon.EnterAsync(new DailyDungeonBossReq { BossId = BossId(weekday) }, GameServer.Options());
            var result = response.Data ?? throw new InvalidOperationException("Daily dungeon entry response is empty.");
            ApplyServerLobby(result.DailyDungeon);
            m_serverEntry = result.Entry;
            m_serverFinish = null;
            m_serverFinishOptions = null;
            m_serverBossHp = m_serverBossMaxHp = 1;
            m_data.percent = 1f;
            return true;
        }
        catch (Exception error) { PopupManager.instance.AlertShow(error.Message); return false; }
        finally { m_serverBusy = false; }
    }

    private async UniTask<bool> CompleteAdServerAsync()
    {
        if (m_serverBusy) return false;
        m_serverBusy = true;
        try
        {
            if (m_pendingAdOptions == null)
            {
                if (!await AdsManager.instance.ShowAsync()) return false;
                m_pendingAdOptions = GameServer.Options();
            }
            var response = await GameServer.DailyDungeon.CompleteAdAsync(new DailyDungeonCompleteAdReq(), m_pendingAdOptions);
            ApplyServerLobby(response.Data?.DailyDungeon ?? throw new InvalidOperationException("Daily dungeon ad response is empty."));
            m_pendingAdOptions = null;
            return true;
        }
        catch (Exception error) { PopupManager.instance.AlertShow(error.Message); return false; }
        finally { m_serverBusy = false; }
    }

    private async UniTask SweepServerAsync(WeekdayType weekday, UnityAction onUpdate)
    {
        if (m_serverBusy) return;
        m_serverBusy = true;
        DailyDungeonRecordData result;
        try
        {
            var bossId = BossId(weekday);
            if (m_pendingSweepOptions == null || m_pendingSweepBoss != bossId)
            {
                m_pendingSweepOptions = GameServer.Options();
                m_pendingSweepBoss = bossId;
            }
            var response = await GameServer.DailyDungeon.SweepAsync(new DailyDungeonBossReq { BossId = bossId }, m_pendingSweepOptions);
            result = ApplyServerReward(response.Data ?? throw new InvalidOperationException("Daily dungeon sweep response is empty."), true);
            m_pendingSweepOptions = null;
        }
        catch (Exception error) { PopupManager.instance.AlertShow(error.Message); return; }
        finally { m_serverBusy = false; }
        TutorialManager.instance.Action_DailyDungeonPlay();
        onUpdate?.Invoke();
        PopupManager.instance.CloseAll();
        await PopupManager.instance.OpenPopupAndWait(PopupType.DailyDungeonResult, result);
    }

    public void SaveResultData(long hp, long maxHp)
    {
        if (!GameServer.Enabled) { SaveResultData(maxHp > 0 ? (float)((double)hp / maxHp) : 1); return; }
        if (maxHp <= 0)
            throw new InvalidOperationException("Daily dungeon HP is invalid.");
        m_serverBossMaxHp = maxHp;
        m_serverBossHp = Math.Max(1, Math.Min(maxHp, hp));
        m_data.percent = (float)((double)m_serverBossHp / m_serverBossMaxHp);
    }

    private async UniTask<DailyDungeonRecordData> FinishServerAsync()
    {
        if (m_serverEntry == null) throw new InvalidOperationException("Daily dungeon has no server entry.");
        if (m_serverFinish == null)
        {
            var completed = (int)m_data.curGradeType - 1;
            var allKilled = completed >= (int)DungeonBossGrade.Legend;
            m_serverFinish = new DailyDungeonFinishReq
            {
                EntryEventId = m_serverEntry.EntryEventId,
                HighestKilledGrade = completed < 0 ? null : (DungeonBossGrade?)Math.Min(completed, (int)DungeonBossGrade.Legend),
                CurrentBossHp = allKilled ? null : m_serverBossHp.ToString(CultureInfo.InvariantCulture),
                CurrentBossMaxHp = allKilled ? null : m_serverBossMaxHp.ToString(CultureInfo.InvariantCulture)
            };
            m_serverFinishOptions = GameServer.Options();
        }
        // Retries of the same result preserve the body and request ID.
        var response = await GameServer.DailyDungeon.FinishAsync(m_serverFinish, m_serverFinishOptions);
        var result = ApplyServerReward(response.Data ?? throw new InvalidOperationException("Daily dungeon finish response is empty."), false);
        m_serverEntry = null;
        return result;
    }

    private DailyDungeonRecordData ApplyServerReward(DailyDungeonRewardRes result, bool sweep)
    {
        ApplyServerLobby(result.DailyDungeon);
        ServerState.ApplyAsset(result.Asset);
        ServerState.ApplyItems(result.ItemUpdates);
        var rewards = new List<ItemData>();
        void AddItem(long id, long count) { if (count > 0) rewards.Add(ServerState.ToItem(id, count)); }
        void AddCurrency(string key, long count)
        {
            if (count <= 0) return;
            var row = GameServer.TableRows("s_item").First(x => (string)x["key"] == key);
            AddItem((long)row["idx"], count);
        }
        AddItem(result.Result.Rewards.ClassItemId, result.Result.Rewards.SoulStoneCount);
        AddCurrency("rice", result.Result.Rewards.Rice);
        AddCurrency("free_gold", result.Result.Rewards.Gold);
        AddCurrency("time_stone", result.Result.Rewards.TimeStoneCount);
        return new DailyDungeonRecordData
        {
            weekday = BossWeekday(result.Result.BossId),
            gradeType = result.Result.HighestKilledGrade.HasValue ? (GradeType)(int)result.Result.HighestKilledGrade.Value : GradeType.NONE,
            percent = sweep ? 0 : 1 - m_data.percent,
            isSweep = sweep, serverRewards = rewards
        };
    }
}
