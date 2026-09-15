using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ThreeKingdoms.Client.Server;
using ThreeKingdoms.Shared.Types;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;
using UnityEngine.Events;

public partial class Data_Castle
{
    public CastleSnapshotDto ServerCastle { get; private set; }
    public CastleBuildingUpgradeLobbyDto ServerUpgrade { get; private set; }
    public CastleAssignmentOutcome LastServerAssignment { get; private set; }
    private readonly SemaphoreSlim serverGate = new SemaphoreSlim(1, 1);

    public sealed class CastleAssignmentOutcome
    {
        public string RequestId;
        public string ExpectedRevision;
        public string AppliedRevision;
        public string ErrorCode;
        public bool ReloadedAfterConflict;
    }

    public async UniTask InitializeServerAsync()
    {
        Release();
        m_db = new Dictionary<CastleObjectType, CastleData>();
        for (var type = CastleObjectType.Palace; type < CastleObjectType.MAX; type++)
            m_db[type] = new CastleData { type = type, level = 1, heroes = new List<string>() };
        mission.InitializeServerView();
        ServerCastle = null;
        ServerUpgrade = null;
        LastServerAssignment = null;
        if (!GameServer.IsLoggedIn || GameServer.Login?.RegionSelected != true) return;
        await RefreshServerAsync();
        m_cts = new CancellationTokenSource();
        PollServerAsync(m_cts.Token).Forget();
    }

    private async UniTask PollServerAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: token);
                if (GameServer.IsLoggedIn)
                {
                    try { await RefreshServerAsync(token); }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception error) { GameServer.Report(error); }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { GameServer.Report(error); }
    }

    public async UniTask RefreshServerAsync(CancellationToken token = default)
    {
        if (!GameServer.IsLoggedIn || GameServer.Login?.RegionSelected != true) return;
        await serverGate.WaitAsync(token);
        try
        {
            var upgrade = await GameServer.Castle.BuildingUpgradeLobbyAsync(new CastleBuildingUpgradeLobbyReq(), GameServer.Options(), token);
            var office = await GameServer.Castle.OfficeLobbyAsync(new CastleOfficeLobbyReq(), GameServer.Options(), token);
            // Office lobby materializes castle production and can advance the shared castle revision.
            // Read the root snapshot last so its revision describes all completed reads in this refresh.
            var castle = await GameServer.Castle.LobbyAsync(new CastleLobbyReq(), GameServer.Options(), token);
            ApplyServerCastle(castle.Data);
            ApplyServerUpgrade(upgrade.Data);
            mission.ApplyServerOffice(office.Data);
        }
        finally { serverGate.Release(); }
    }

    public static long ServerCharacterId(string key) => ServerState.CharacterId(key);
    public static string ServerCharacterKey(long id) => ServerState.Characters.Characters.Single(row => row.CharacterId == id).CharacterKey;
    public static long ServerTicks(string value) => string.IsNullOrEmpty(value) ? 0 : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal).Ticks;
    private static float Number(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    private static CastleObjectType BuildingType(string key) => (CastleObjectType)Enum.Parse(typeof(CastleObjectType), key, true);
    public CastleBuildingDto ServerBuilding(CastleObjectType type) => ServerCastle?.Buildings?.SingleOrDefault(row => BuildingType(row.BuildingKey) == type);
    public float ServerProductionRate(CastleObjectType type) => ServerBuilding(type)?.Production == null ? 0 : Number(ServerBuilding(type).Production.ProductionPerSeconds);
    public int ServerStorageCap(CastleObjectType type) => ServerBuilding(type)?.Production == null ? 0 : Mathf.FloorToInt(Number(ServerBuilding(type).Production.StorageCap));

    public bool CanServerUpgrade(CastleObjectType type)
    {
        if (type == CastleObjectType.Office) return false; // Office levels are granted by mission XP.
        var current = ServerBuilding(type);
        if (current == null) return false;
        var active = ServerUpgrade?.Upgrades?.SingleOrDefault(row => row.BuildingId == current.BuildingId);
        if (active != null) return active.Status == CastleBuildingUpgradeStatus.Running;
        if (!ServerUpgradePrerequisitesMet(type)) return false;
        return ServerUpgradeRequirements(type).All(row => row.FulfillmentRate == "1.000000");
    }

    public bool ServerUpgradePrerequisitesMet(CastleObjectType type)
    {
        var current = ServerBuilding(type);
        if (current == null || type == CastleObjectType.Office) return false;
        var master = GameServer.TableRows("s_building").Single(row => (long)row["idx"] == current.BuildingId);
        if (current.Level >= (long)master["max_level"]) return false;
        var palace = ServerBuilding(CastleObjectType.Palace);
        return type == CastleObjectType.Palace
            ? ServerCastle.Buildings.Where(row => row.BuildingId != palace.BuildingId).All(row => row.Level >= current.Level)
            : current.Level + 1 <= palace.Level;
    }

    public IReadOnlyList<CastleRequirementDto> ServerUpgradeRequirements(CastleObjectType type)
    {
        var current = ServerBuilding(type);
        if (current == null || type == CastleObjectType.Office) return Array.Empty<CastleRequirementDto>();
        var master = GameServer.TableRows("s_building").Single(row => (long)row["idx"] == current.BuildingId);
        if (current.Level >= (long)master["max_level"]) return Array.Empty<CastleRequirementDto>();
        var levels = GameServer.TableRows("s_building_level").Where(row => (long)row["level"] == current.Level + 1).ToArray();
        var level = levels.SingleOrDefault(row => (string)row["building_key"] == current.BuildingKey);
        // The API table catalog uses the shared default row for non-palace buildings.
        if (level == null && current.BuildingKey != "palace")
            level = levels.SingleOrDefault(row => (string)row["building_key"] == "default");
        if (level == null) throw new InvalidOperationException("CASTLE_BUILDING_LEVEL_NOT_FOUND:" + current.BuildingKey + ":" + (current.Level + 1));
        var palaceRate = decimal.Parse(ServerBuilding(CastleObjectType.Palace).Requirements.Single(row => row.StatType == StatType.Charisma).FulfillmentRate, CultureInfo.InvariantCulture);
        var multiplier = type == CastleObjectType.Palace ? 1m : 2m - palaceRate;
        var result = new List<CastleRequirementDto>();
        for (var index = 1; index <= 2; index++)
        {
            var stat = (StatType)(int)master["stat_type_" + index];
            if (stat == StatType.None) continue;
            var actual = current.Requirements.Single(row => row.StatType == stat).CurrentValue;
            var required = (decimal)level["req_stat_value_" + index] * multiplier;
            if (required <= 0) throw new InvalidOperationException("CASTLE_UPGRADE_REQUIREMENT_INVALID");
            var rate = Math.Min(1m, decimal.Round(decimal.Parse(actual, CultureInfo.InvariantCulture) / required, 6, MidpointRounding.AwayFromZero));
            result.Add(new CastleRequirementDto { StatType = stat, CurrentValue = actual, RequiredValue = required.ToString("0.000000", CultureInfo.InvariantCulture), FulfillmentRate = rate.ToString("0.000000", CultureInfo.InvariantCulture) });
        }
        return result;
    }

    public void ApplyServerCastle(CastleSnapshotDto snapshot)
    {
        if (snapshot == null) throw new InvalidOperationException("CASTLE_SNAPSHOT_REQUIRED");
        if (ServerCastle != null && System.Numerics.BigInteger.Parse(snapshot.Revision) < System.Numerics.BigInteger.Parse(ServerCastle.Revision)) return;
        var previousThief = ServerCastle?.Thief?.Active?.ThiefInstanceId;
        ServerCastle = snapshot;
        foreach (var row in snapshot.Buildings)
        {
            var type = BuildingType(row.BuildingKey);
            var previousLevel = m_db[type].level;
            var data = m_db[type];
            data.level = checked((int)row.Level);
            data.heroes = row.AssignedCharacterIds.Select(ServerCharacterKey).ToList();
            data.totalAmount = row.Production == null ? 0 : Number(row.Production.ProductionAmount);
            data.tickClaim = ServerTicks(row.Production?.SettledAt ?? snapshot.ServerTime);
            m_db[type] = data;
            Signal.instance.UpdateCastleHeroBatch.Emit(data.DeepClone());
            if (row.Production != null) Signal.instance.UpdateFarmMarketData.Emit(data.DeepClone());
            if (data.level > previousLevel) Signal.instance.CompleteCaslteBuildingUpgrade.Emit(data.DeepClone());
        }
        var thief = snapshot.Thief;
        m_wallyData = new CastleWallyData
        {
            tickNextCheck = ServerTicks(thief.NextSpawnCheckAt), tickSpawn = ServerTicks(thief.Active?.SpawnedAt),
            tickEndSpawn = ServerTicks(thief.Active?.ExpiresAt)
        };
        if (thief.Active != null && thief.Active.ThiefInstanceId != previousThief) Signal.instance.CastleWally_Spawn.Emit();
        if (thief.Active == null && previousThief != null) Signal.instance.CastleWally_Failed.Emit();
    }

    public void ApplyServerUpgrade(CastleBuildingUpgradeLobbyDto snapshot)
    {
        if (snapshot != null && ServerUpgrade != null && ServerTicks(snapshot.StateAt) < ServerTicks(ServerUpgrade.StateAt)) return;
        ServerUpgrade = snapshot ?? throw new InvalidOperationException("CASTLE_UPGRADE_SNAPSHOT_REQUIRED");
        foreach (var data in m_db.Values)
        {
            var upgrade = snapshot.Upgrades.SingleOrDefault(row => BuildingType(row.BuildingKey) == data.type);
            data.tickUpgradeEnd = upgrade == null ? 0 : ServerTicks(upgrade.CompletesAt);
            data.remainUpgradeSeconds = upgrade?.Status == CastleBuildingUpgradeStatus.Paused ? upgrade.RemainingSeconds : 0;
            if (upgrade != null && data.tickUpgradeEnd == 0) data.tickUpgradeEnd = Utils.GetUTC().AddSeconds(upgrade.RemainingSeconds).Ticks;
        }
        building.UpdateBuildingUpgrade(CastleObjectType.NONE);
    }

    private async UniTask SetServerCharactersAsync(CastleData data, UnityAction<StatusType> callback)
    {
        try
        {
            // Keep polling from advancing the server revision between reading it and assigning characters.
            await serverGate.WaitAsync();
            try
            {
                var options = GameServer.Options();
                LastServerAssignment = new CastleAssignmentOutcome { RequestId = options.RequestId, ExpectedRevision = ServerCastle.Revision };
                var response = await GameServer.Castle.BuildingSetCharactersAsync(new CastleSetCharactersReq
                { BuildingId = ServerBuilding(data.type).BuildingId, CharacterIds = data.heroes.Select(ServerCharacterId).ToList(), ExpectedCastleRevision = LastServerAssignment.ExpectedRevision }, options);
                LastServerAssignment.AppliedRevision = response.Data.Revision;
                ApplyServerCastle(response.Data);
            }
            finally { serverGate.Release(); }
            await RefreshServerAsync();
            callback?.Invoke(StatusType.Success);
        }
        catch (GameServerException error) when (error.Code == "CASTLE_STATE_CHANGED")
        {
            if (LastServerAssignment != null) LastServerAssignment.ErrorCode = error.Code;
            try
            {
                await RefreshServerAsync();
                if (LastServerAssignment != null) LastServerAssignment.ReloadedAfterConflict = true;
                PopupManager.instance.AlertShow("영지 상태가 변경되어 다시 불러왔습니다. 배치를 확인한 후 다시 시도해주세요.");
            }
            catch (Exception refreshError) { GameServer.Report(refreshError); }
            GameServer.Report(error);
            callback?.Invoke(StatusType.Failed);
        }
        catch (Exception error) { GameServer.Report(error); callback?.Invoke(StatusType.Failed); }
    }

    private async UniTask CollectServerAsync(CastleObjectType type, UnityAction<StatusType> callback)
    {
        try
        {
            var response = await GameServer.Castle.BuildingCollectAsync(new CastleCollectReq { BuildingId = ServerBuilding(type).BuildingId }, GameServer.Options());
            ServerState.ApplyAsset(response.Data.Asset);
            ApplyServerCastle(response.Data.Castle);
            callback?.Invoke(StatusType.Success);
        }
        catch (Exception error) { GameServer.Report(error); callback?.Invoke(StatusType.Failed); }
    }

    public async UniTask<bool> CaptureServerThiefAsync()
    {
        if (ServerCastle?.Thief?.Active == null) return false;
        try
        {
            var response = await GameServer.Castle.ThiefCaptureAsync(new CastleThiefCaptureReq { ThiefInstanceId = ServerCastle.Thief.Active.ThiefInstanceId }, GameServer.Options());
            ApplyServerCastle(response.Data.Castle);
            ServerState.ApplyAsset(response.Data.Asset);
            return response.Data.Captured;
        }
        catch (Exception error) { GameServer.Report(error); return false; }
    }

    public async UniTask ClearServerDebuffAsync()
    {
        var response = await GameServer.Castle.ThiefClearDebuffAsync(new CastleThiefClearDebuffReq(), GameServer.Options());
        ApplyServerCastle(response.Data.Castle);
    }
}

public partial class Data_Castle_Building
{
    private void UpdateServerUpgradeView(CastleObjectType type)
    {
        if (type == CastleObjectType.NONE)
        {
            for (var current = CastleObjectType.Palace; current < CastleObjectType.MAX; current++) UpdateServerUpgradeView(current);
            return;
        }
        Release_CTS(type);
        var data = DataManager.castle.GetCaslteData(type);
        if (!data.isDoingUpgrade) return;
        if (data.remainUpgradeSeconds > 0) { Signal.instance.StopCaslteBuildingUpgrade.Emit(data.DeepClone()); return; }
        Signal.instance.StartCaslteBuildingUpgrade.Emit(data.DeepClone());
        var source = new CancellationTokenSource();
        m_cts[type] = source;
        ServerUpgradeTimerAsync(type, source.Token).Forget();
    }

    private async UniTask ServerUpgradeTimerAsync(CastleObjectType type, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var view = GetUpgradeData(DataManager.castle.GetCaslteData(type));
                if (view.ts < TimeSpan.Zero) view.ts = TimeSpan.Zero;
                Signal.instance.UpdateCaslteBuildingUpgrade.Emit(view);
                await UniTask.Delay(250, cancellationToken: token);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async UniTask StartServerUpgradeAsync(CastleObjectType type, UnityAction<Data_Castle.CastleData> callback)
    {
        try
        {
            var response = await GameServer.Castle.BuildingUpgradeStartAsync(new CastleBuildingUpgradeStartReq { BuildingId = DataManager.castle.ServerBuilding(type).BuildingId }, GameServer.Options());
            DataManager.castle.ApplyServerUpgrade(response.Data.BuildingUpgrade);
            callback?.Invoke(DataManager.castle.GetCaslteData(type));
        }
        catch (Exception error) { GameServer.Report(error); }
    }

    private async UniTask ShortenServerUpgradeAsync(CastleObjectType type, bool isAd, int count)
    {
        try
        {
            var id = DataManager.castle.ServerBuilding(type).BuildingId;
            if (isAd)
            {
                var response = await GameServer.Castle.BuildingUpgradeCompleteAdAsync(new CastleBuildingUpgradeCompleteAdReq { BuildingId = id }, GameServer.Options());
                DataManager.castle.ApplyServerCastle(response.Data.Castle);
                DataManager.castle.ApplyServerUpgrade(response.Data.BuildingUpgrade);
            }
            else
            {
                var response = await GameServer.Castle.BuildingUpgradeShortenAsync(new CastleBuildingUpgradeShortenReq { BuildingId = id, TimeStoneCount = count }, GameServer.Options());
                ServerState.ApplyAsset(response.Data.Asset);
                DataManager.castle.ApplyServerCastle(response.Data.Castle);
                DataManager.castle.ApplyServerUpgrade(response.Data.BuildingUpgrade);
            }
        }
        catch (Exception error) { GameServer.Report(error); }
    }
}

public partial class Data_Castle_Mission
{
    private readonly Dictionary<string, int> serverMissionIds = new Dictionary<string, int>();
    private int nextServerMissionId;
    public CastleOfficeLobbyDto ServerOffice { get; private set; }
    public event Action ServerOfficeChanged;

    public void InitializeServerView()
    {
        ServerOffice = null;
        serverMissionIds.Clear();
        nextServerMissionId = 0;
        m_data = new List<CastleMissionData>();
        m_levelInfo = new CastleMissionLevelInfoData { level = 1, maxExp = int.MaxValue, tickMission = Utils.GetUTC().Ticks };
    }

    private int MissionIndex(string id)
    {
        if (!serverMissionIds.TryGetValue(id, out var index)) serverMissionIds[id] = index = ++nextServerMissionId;
        return index;
    }

    public void ApplyServerOffice(CastleOfficeLobbyDto office)
    {
        if (office == null) throw new InvalidOperationException("CASTLE_OFFICE_SNAPSHOT_REQUIRED");
        if (ServerOffice != null && System.Numerics.BigInteger.Parse(office.Revision) < System.Numerics.BigInteger.Parse(ServerOffice.Revision)) return;
        ServerOffice = office;
        m_levelInfo = new CastleMissionLevelInfoData
        {
            level = checked((int)office.Level), nowExp = checked((int)office.Xp), maxExp = checked((int)(office.NextLevelXp ?? int.MaxValue)),
            missionCount = checked((int)office.RemainingCount), tickMission = Data_Castle.ServerTicks(office.StateAt)
        };
        var data = new List<CastleMissionData>();
        foreach (var offer in office.Offers)
            data.Add(new CastleMissionData
            {
                idx = MissionIndex(offer.OfferId), serverOfferId = offer.OfferId, key = offer.MissionKey,
                grade = (GradeType)((int)offer.MissionGrade * 2), heroes = new List<string>(), rewardKey = new List<string>(),
                serverRequiredStat = checked((int)offer.RequiredStatValue), serverXp = checked((int)offer.OfficeXpReward), serverDuration = checked((int)offer.DurationSeconds)
            });
        foreach (var run in office.Runs)
            data.Add(new CastleMissionData
            {
                idx = MissionIndex(run.RunId), serverOfferId = run.OfferId, serverRunId = run.RunId, serverCompleted = run.IsCompleted,
                key = run.MissionKey, grade = (GradeType)((int)run.MissionGrade * 2), heroes = run.CharacterIds.Select(Data_Castle.ServerCharacterKey).ToList(),
                tickStart = Data_Castle.ServerTicks(run.StartedAt), tickEnd = Data_Castle.ServerTicks(run.CompletesAt), percentStat = (float)run.AchievementRate * 100,
                serverRequiredStat = checked((int)run.RequiredStatValue), serverXp = checked((int)run.OfficeXpReward), serverDuration = checked((int)run.DurationSeconds), rewardKey = new List<string>()
            });
        m_data = data;
        ServerOfficeChanged?.Invoke();
    }

    public async UniTask RefreshServerOffersAsync()
    {
        try
        {
            var response = await GameServer.Castle.OfficeRefreshAsync(new CastleOfficeLobbyReq(), GameServer.Options());
            ServerState.ApplyAsset(response.Data.Asset);
            ApplyServerOffice(response.Data.Office);
        }
        catch (Exception error) { GameServer.Report(error); }
    }

    private async UniTask StartServerMissionAsync(CastleMissionData data, UnityAction<StatusType> callback)
    {
        try
        {
            var response = await GameServer.Castle.OfficeStartAsync(new CastleOfficeStartReq
            { OfferId = data.serverOfferId, CharacterIds = data.heroes.Select(Data_Castle.ServerCharacterId).ToList() }, GameServer.Options());
            ApplyServerOffice(response.Data.Office);
            callback?.Invoke(StatusType.Success);
        }
        catch (Exception error) { GameServer.Report(error); callback?.Invoke(StatusType.Failed); }
    }

    private async UniTask<List<ItemData>> ClaimServerMissionsAsync(UnityAction<StatusType, int> callback, CastleMissionData[] requested)
    {
        var rewards = new List<ItemData>();
        var runs = requested.Length == 0 ? GetFinishedMissions() : requested;
        var previousXp = m_levelInfo.nowExp;
        try
        {
            foreach (var run in runs)
            {
                var response = await GameServer.Castle.OfficeClaimAsync(new CastleOfficeClaimReq { RunId = run.serverRunId }, GameServer.Options());
                ServerState.ApplyAsset(response.Data.Asset);
                ServerState.ApplyItems(response.Data.ItemUpdates);
                if (response.Data.CharacterSnapshot != null) await ServerState.ApplyCharactersAsync(response.Data.CharacterSnapshot);
                rewards.AddRange(response.Data.Rewards.Select(reward => ServerState.ToItem(reward.ItemId, reward.Amount)));
                ApplyServerOffice(response.Data.Office);
            }
            callback?.Invoke(StatusType.Success, Math.Max(0, m_levelInfo.nowExp - previousXp));
        }
        catch (Exception error) { GameServer.Report(error); callback?.Invoke(StatusType.Failed, 0); }
        return rewards;
    }
}
