using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ThreeKingdoms.Client.Server;
using ThreeKingdoms.Shared.Types;
using UnityEngine;

public partial class Data_BossRaid
{
    public RaidStateRes ServerRaid { get; private set; }
    public RaidRoundDto ServerRound { get; private set; }
    public RaidRankRes ServerRanking { get; private set; }
    public RaidClaimRes ServerClaim { get; private set; }
    public bool ServerJoined { get; private set; }
    public string ServerBattleId { get; private set; }
    private RaidSocketClient raidSocket;
    private float roundReceivedAt;
    private double roundRemainingSeconds;
    private long lastRoundServerTicks;
    private long lastStateServerTicks;
    private RaidDamageOutbox damageOutbox;
    private long damageOwnerUid;
    private int damageLifetime;
    private bool sendingDamage;
    public int ServerPendingDamageCount => damageOutbox?.Count ?? 0;
    public event Action ServerRaidChanged;
    public bool ServerSocketConnected => raidSocket?.IsConnected == true;
    public int ServerSocketGeneration => raidSocket?.Generation ?? 0;
    public int ServerSocketRestoredGeneration { get; private set; }

    public async UniTask RefreshServerConnectionAsync()
    {
        if (!ServerJoined || raidSocket == null) throw new GameServerException("RAID_NOT_JOINED", 0);
        await raidSocket.CloseAsync(m_cts.Token);
        // Keep the participant/session intact. The existing loop (or pending request retry) reconnects.
    }

    public double ServerRemainingSeconds => ServerRound?.Phase == "transition" ? roundRemainingSeconds : Math.Max(0, roundRemainingSeconds - (Time.realtimeSinceStartup - roundReceivedAt));
    public DateTime ServerNow => new DateTime(lastRoundServerTicks, DateTimeKind.Utc).AddSeconds(Time.realtimeSinceStartup - roundReceivedAt);

    private async UniTask InitializeServerAsync()
    {
        ReleaseServer();
        m_data = new BossRaidData { keyBoss = "LuBu", gradeMin = GradeType.Normal, gradeMax = GradeType.Normal, nowGrade = GradeType.Normal };
        m_rankPoint = new RankerData { ranker = new List<RankerUserData>(), my = EmptyRank() };
        m_rankPrevRaid = new RankerData { ranker = new List<RankerUserData>(), my = EmptyRank() };
        m_cts = new CancellationTokenSource();
        if (!GameServer.IsLoggedIn) return;
        damageOwnerUid = GameServer.Uid;
        var damageCacheKey = "PP_SERVER_RAID_DAMAGE_" + damageOwnerUid;
        damageOutbox = new RaidDamageOutbox(PPWorker.Get<List<JObject>>(damageCacheKey), rows => PPWorker.Set(damageCacheKey, rows));
        await RefreshServerAsync();
        StartDamageSender();
        AddressableManager.instance.Load_HeroCharacterAsync(m_data.keyBoss + "_BossRaid").Forget();
        ServerLoopAsync(m_cts.Token).Forget();
    }

    private async UniTask ServerLoopAsync(CancellationToken token)
    {
        var nextRefresh = Time.realtimeSinceStartup + 3;
        try
        {
            while (!token.IsCancellationRequested)
            {
                StartDamageSender();
                while (raidSocket != null && raidSocket.TryReadBroadcast(out var message))
                    ApplyServerRound(message["data"]["round"].ToObject<RaidRoundDto>(), (string)message["data"]["server_time"]);
                remainSeconds = ServerRemainingSeconds;
                if (Time.realtimeSinceStartup >= nextRefresh)
                {
                    nextRefresh = Time.realtimeSinceStartup + 3;
                    try
                    {
                        await RefreshServerAsync();
                        if (ServerJoined) await RefreshServerRankAsync(ServerBattleId);
                        if (ServerJoined && (raidSocket?.IsConnected != true || ServerSocketRestoredGeneration != ServerSocketGeneration) && ServerRound?.Phase != "finished")
                        {
                            var activeSocket = EnsureRaidSocket();
                            await activeSocket.ConnectAsync(token);
                            ApplyServerState(await activeSocket.RequestAsync<RaidStateRes>(new JObject
                            { ["type"] = "RAID_JOIN", ["request_id"] = Guid.NewGuid().ToString(), ["raid_id"] = ServerBattleId }, token), true);
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception error) { GameServer.Report(error); }
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException) { }
    }

    public async UniTask RefreshServerAsync()
    {
        var battleId = ServerJoined ? ServerBattleId : null;
        var state = !string.IsNullOrEmpty(battleId)
            ? (await GameServer.Raid.JoinAsync(new RaidJoinReq { RaidId = battleId }, GameServer.Options())).Data
            : (await GameServer.Raid.StateAsync(new RaidStateReq(), GameServer.Options())).Data;
        if ((ServerJoined ? ServerBattleId : null) != battleId) return;
        ApplyServerState(state);
    }

    public async UniTask<bool> JoinServerAsync()
    {
        try
        {
            await RefreshServerAsync();
            if (ServerRound == null) throw new GameServerException("RAID_NOT_READY", 0);
            var response = await GameServer.Raid.JoinAsync(new RaidJoinReq { RaidId = ServerRound.RaidId }, GameServer.Options());
            ApplyServerState(response.Data);
            ServerBattleId = response.Data.Round.RaidId;
            ServerClaim = null;
            ServerJoined = true;
            raidSocket?.Dispose();
            raidSocket = new RaidSocketClient();
            ServerSocketRestoredGeneration = 0;
            await raidSocket.ConnectAsync(m_cts.Token);
            ApplyServerState(await raidSocket.RequestAsync<RaidStateRes>(new JObject
            { ["type"] = "RAID_JOIN", ["request_id"] = Guid.NewGuid().ToString(), ["raid_id"] = ServerBattleId }, m_cts.Token), true);
            await RefreshServerRankAsync(ServerBattleId);
            return true;
        }
        catch (Exception error)
        {
            ServerJoined = false;
            ServerBattleId = null;
            if (ServerPendingDamageCount == 0) { raidSocket?.Dispose(); raidSocket = null; }
            GameServer.Report(error);
            return false;
        }
    }

    private void ApplyServerState(RaidStateRes state, bool fromSocket = false)
    {
        if (state.Round != null && !RaidRoundOrder.Allows(ServerRound?.RaidId, state.Round.RaidId, ServerJoined ? ServerBattleId : null)) return;
        var at = Data_Castle.ServerTicks(state.ServerTime);
        if (state.Round?.RaidId == ServerRaid?.Round?.RaidId && at < lastStateServerTicks) return;
        lastStateServerTicks = at;
        ServerRaid = state;
        ServerState.ApplyRaidPoints(state.RaidPointBalance);
        if (state.Round != null) ApplyServerRound(state.Round, state.ServerTime);
        if (fromSocket) ServerSocketRestoredGeneration = ServerSocketGeneration;
    }

    private void ApplyServerRound(RaidRoundDto round, string serverTime)
    {
        var at = Data_Castle.ServerTicks(serverTime);
        if (!RaidRoundOrder.Allows(ServerRound?.RaidId, round.RaidId, ServerJoined ? ServerBattleId : null)) return;
        if (ServerRound?.RaidId == round.RaidId)
        {
            var revision = System.Numerics.BigInteger.Parse(round.Revision).CompareTo(System.Numerics.BigInteger.Parse(ServerRound.Revision));
            if (revision < 0 || (revision == 0 && at < lastRoundServerTicks)) return;
        }
        lastRoundServerTicks = at;
        var previousPhase = ServerRound?.Phase;
        ServerRound = round;
        roundReceivedAt = Time.realtimeSinceStartup;
        roundRemainingSeconds = round.RemainingMilliseconds / 1000d;
        remainSeconds = roundRemainingSeconds;
        m_data.keyBoss = round.BossKey;
        m_data.nowGrade = (GradeType)round.Difficulty;
        m_data.tickNextRound = Data_Castle.ServerTicks(round.StartsAt);
        m_data.tickEndRound = Data_Castle.ServerTicks(round.EndsAt);
        if (round.Phase == "jin") m_data.tickSecondPhase = Data_Castle.ServerTicks(round.PhaseStartedAt);
        else if (round.Phase == "waiting" || round.Phase == "normal") m_data.tickSecondPhase = 0;
        var current = DateTime.Parse(serverTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        m_data.tickEndSeason = new DateTime(current.Year, current.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).Ticks;
        m_raidStatus = round.Phase switch
        {
            "normal" => BossRaidStatusType.FirstPhase, "transition" => BossRaidStatusType.Finish_FirstPhase,
            "jin" => BossRaidStatusType.SecondPhase, "finished" => BossRaidStatusType.Finished, _ => BossRaidStatusType.Wait
        };
        if (previousPhase != round.Phase) Signal.instance.BossRaidStatus.Emit(m_raidStatus);
        Signal.instance.UpdageBossHP.Emit(((float)(long.Parse(round.Hp) / (double)long.Parse(round.MaxHp)), long.Parse(round.MaxHp)));
        ServerRaidChanged?.Invoke();
    }

    public void EmitServerPhase() => Signal.instance.BossRaidStatus.Emit(m_raidStatus);

    public void QueueServerDamage(long damage)
    {
        if (!ServerJoined || damage <= 0 || (ServerRound?.Phase != "normal" && ServerRound?.Phase != "jin")) return;
        damageOutbox.Enqueue(new JObject
        {
            ["type"] = "RAID_DAMAGE", ["request_id"] = Guid.NewGuid().ToString(), ["raid_id"] = ServerBattleId,
            ["phase"] = ServerRound.Phase, ["damage"] = damage.ToString(CultureInfo.InvariantCulture)
        });
        StartDamageSender();
    }

    private RaidSocketClient EnsureRaidSocket()
    {
        if (raidSocket == null || raidSocket.IsDisposed)
        {
            raidSocket = new RaidSocketClient();
            ServerSocketRestoredGeneration = 0;
        }
        return raidSocket;
    }

    private void StartDamageSender()
    {
        if (!sendingDamage && ServerPendingDamageCount > 0 && m_cts != null && !m_cts.IsCancellationRequested
            && GameServer.IsLoggedIn && GameServer.Uid == damageOwnerUid)
            SendServerDamageAsync().Forget();
    }

    private async UniTask SendServerDamageAsync()
    {
        var outbox = damageOutbox;
        var owner = damageOwnerUid;
        var generation = damageLifetime;
        using var stopped = CancellationTokenSource.CreateLinkedTokenSource(m_cts.Token);
        var token = stopped.Token;
        var nextFailureLog = 0f;
        sendingDamage = true;
        try
        {
            await outbox.DrainAsync(async (request, cancellation) =>
                {
                    await UniTask.SwitchToMainThread();
                    if (generation != damageLifetime || GameServer.Uid != owner) stopped.Cancel();
                    cancellation.ThrowIfCancellationRequested();
                    var state = await EnsureRaidSocket().RequestAsync<RaidStateRes>(request, cancellation);
                    await UniTask.SwitchToMainThread();
                    return state;
                },
                error => error is GameServerException server && RaidDamageOutbox.IsDefinitiveRejection(server.Code),
                (request, state) =>
                {
                    if (generation != damageLifetime || GameServer.Uid != owner) return;
                    try
                    {
                        // A late receipt must not overwrite a new round or an exited battle's lobby snapshot.
                        if (ServerJoined && ServerBattleId == (string)request["raid_id"]) ApplyServerState(state, true);
                    }
                    catch (Exception error) { GameServer.Report(error); }
                },
                (request, error, rejected) =>
                {
                    if (rejected || Time.realtimeSinceStartup >= nextFailureLog)
                    {
                        Debug.LogWarning($"[RAID_DAMAGE_{(rejected ? "REJECTED" : "PENDING")}] raid={request["raid_id"]} request_id={request["request_id"]} reason={error.Message}");
                        nextFailureLog = Time.realtimeSinceStartup + 3;
                    }
                }, cancellation => System.Threading.Tasks.Task.Delay(500, cancellation), token);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { GameServer.Report(error); }
        finally
        {
            if (generation == damageLifetime)
            {
                sendingDamage = false;
                if (!ServerJoined && ServerPendingDamageCount == 0) { raidSocket?.Dispose(); raidSocket = null; }
            }
        }
    }

    private RankerUserData EmptyRank() => new RankerUserData { uid = DataManager.userInfo.uid, nickname = DataManager.userInfo.nickname, skin = "LuBu", point = 0, rank = 0 };
    private RankerUserData RankUser(RaidRankEntryDto user) => user == null ? EmptyRank() : new RankerUserData
    {
        uid = checked((int)user.Uid), nickname = user.Nickname,
        point = long.TryParse(user.Damage, out var number) ? number : long.MaxValue,
        serverDamage = user.Damage, serverPercentile = user.Percentile, rank = user.Rank, skin = "LuBu"
    };

    public async UniTask RefreshServerRankAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        ServerRanking = (await GameServer.Raid.RankAsync(new RaidRankReq { RaidId = id }, GameServer.Options())).Data;
        m_rankNow = ServerRanking.Top.Concat(ServerRanking.AroundMe).GroupBy(row => row.Uid).Select(group => RankUser(group.First())).OrderBy(row => row.rank).ToList();
        var rank = new RankerData { ranker = m_rankNow.ToList(), my = RankUser(ServerRanking.Me) };
        m_rankPoint = rank;
        m_rankPrevRaid = rank;
        if (BossRaidWorker.instance.isRunning && RankBossRaidComponent.instance != null) RankBossRaidComponent.instance.UpdateRanker();
    }

    private async UniTask LoadServerRanksAsync()
    {
        await RefreshServerAsync();
        await RefreshServerRankAsync(ServerRound?.RaidId);
        var current = m_rankPoint;
        var lastId = ServerRaid?.LastParticipatedRaidId;
        if (!string.IsNullOrEmpty(lastId) && lastId != ServerRound?.RaidId) await RefreshServerRankAsync(lastId);
        else if (string.IsNullOrEmpty(lastId)) m_rankPrevRaid = new RankerData { ranker = new List<RankerUserData>(), my = EmptyRank() };
        m_rankPoint = current;
    }

    public async UniTask<RaidClaimRes> ClaimServerAsync()
    {
        var id = ServerBattleId ?? ServerRaid?.LastParticipatedRaidId;
        if (string.IsNullOrEmpty(id)) throw new GameServerException("RAID_NOT_PARTICIPATED", 0);
        ServerClaim = (await GameServer.Raid.ClaimAsync(new RaidClaimReq { RaidId = id }, GameServer.Options())).Data;
        ServerState.ApplyRaidPoints(ServerClaim.RaidPointBalance);
        await RefreshServerRankAsync(id);
        return ServerClaim;
    }

    public void ExitServerBattle()
    {
        ServerJoined = false;
        ServerBattleId = null;
        if (ServerPendingDamageCount == 0) { raidSocket?.Dispose(); raidSocket = null; }
        else StartDamageSender();
        RefreshServerAsync().Forget();
    }

    private void ReleaseServer()
    {
        damageLifetime++;
        m_cts = m_cts.ReleaseCTS();
        raidSocket?.Dispose();
        raidSocket = null;
        ServerJoined = false;
        ServerBattleId = null;
        damageOutbox = null;
        sendingDamage = false;
    }
}
