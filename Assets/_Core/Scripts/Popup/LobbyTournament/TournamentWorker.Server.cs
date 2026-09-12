using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using ThreeKingdoms.Client.Server;
using ThreeKingdoms.Shared.Enums;
using ThreeKingdoms.Shared.Rest;
using ThreeKingdoms.Shared.Types;
using UnityEngine;

namespace Rev9.Tournament
{
    public partial class TournamentWorker
    {
        private TournamentSnapshotDto m_serverSnapshot;
        private CharacterSnapshotDto m_serverCharacters;
        private TournamentEntryDto m_serverEntry;
        private TournamentFinishRes m_serverResult;
        private RankerUserData m_serverPreviousRank;
        private readonly SemaphoreSlim m_serverActionGate = new(1, 1);
        private readonly Dictionary<string, RestRequestOptions> m_pendingServerRequests = new();
        private readonly Dictionary<int, string> m_serverHistoryIds = new();
        private bool m_serverFinishing;
        private bool m_enteringServer;
        public TournamentEntryDto ServerEntry => m_serverEntry;
        public TournamentFinishRes ServerResult => m_serverResult;
        public float BattleDurationSeconds => GameServer.Enabled && m_serverEntry != null ? m_serverEntry.BattleDurationSeconds : 60;
        public string ServerTierLabel => m_serverSnapshot?.User == null ? "-" : "Tier " + m_serverSnapshot.User.TierIdx;
        public bool ServerAvailable => m_serverSnapshot?.SeasonStatus == TournamentAvailabilityStatus.Open && m_serverSnapshot.User != null;
        public RankerUserData PreviousResultRank => m_serverPreviousRank ?? rankData;
        public bool ServerResultIsWin => m_serverResult?.Result == TournamentBattleResult.Win;
        public long ServerRefreshPrice => Parameter("TOURNAMENT_REFRESH_PAID_COST_BASE", 50)
            + Parameter("TOURNAMENT_REFRESH_PAID_COST_PER_STEP", 50) * (m_serverSnapshot?.User?.PaidRefreshCount ?? 0);

        private static long Parameter(string key, long fallback)
        {
            var row = GameServer.TableRows("s_parameter").FirstOrDefault(x => (string)x["key"] == key);
            return row == null ? fallback : long.Parse((string)row["value"], CultureInfo.InvariantCulture);
        }
        private RestRequestOptions PendingOptions(string key)
        {
            if (!m_pendingServerRequests.TryGetValue(key, out var options))
                m_pendingServerRequests[key] = options = GameServer.Options();
            return options;
        }
        private static string ProfileKey(long? id) => id.HasValue
            ? (string)GameServer.TableRows("s_character").FirstOrDefault(x => (long)x["idx"] == id.Value)?["key"]
            : null;
        private static long CharacterId(string key) => (long)(GameServer.TableRows("s_character").FirstOrDefault(x => (string)x["key"] == key)?["idx"]
            ?? throw new InvalidOperationException("Unknown tournament hero: " + key));
        private static long TreasureId(string key) => (long)(GameServer.TableRows("s_treasure").FirstOrDefault(x => (string)x["key"] == key)?["idx"]
            ?? throw new InvalidOperationException("Unknown tournament treasure: " + key));
        private static string TreasureKey(long id) => (string)(GameServer.TableRows("s_treasure").FirstOrDefault(x => (long)x["idx"] == id)?["key"]
            ?? throw new InvalidOperationException("Unknown tournament treasure: " + id));
        private static TournamentBatchData EmptyTeam(long uid)
        {
            var result = new TournamentBatchData { uid = checked((int)uid) };
            result.Default();
            return result;
        }

        private async UniTask InitializeServerAsync()
        {
            var characters = await GameServer.Character.LobbyAsync(new EmptyRes(), GameServer.Options());
            m_serverCharacters = characters.Data ?? throw new InvalidOperationException("Character snapshot is empty.");
            await ServerState.ApplyCharactersAsync(m_serverCharacters);
            await ReloadServerSnapshotAsync();
            if (ServerAvailable && m_serverSnapshot.User.AttackFormation == null && m_serverCharacters.Characters.Count > 0)
            {
                var defaultTeam = EmptyTeam(GameServer.Uid);
                var positions = new[] { 1, 3, 5, 7 };
                foreach (var character in m_serverCharacters.Characters.Take(4))
                {
                    var hero = ServerState.ToHero(character);
                    hero.sortIdx = positions[defaultTeam.heroes.Count];
                    defaultTeam.heroes.Add(hero);
                }
                await SaveTeamServerAsync(true, defaultTeam);
            }
            await LoadServerRankingAsync(PopupLobbyBossRaid_PopupRanking.TabType.Tutorial_Point);
            Signal.instance.DayChange.connect = () => ReloadServerSnapshotWithAlertAsync().Forget();
            if (m_data.countRefresh < Parameter("TOURNAMENT_REFRESH_FREE_LIMIT_COUNT", 3)) TimerServerRefreshAsync().Forget();
        }

        private async UniTask ReloadServerSnapshotAsync()
        {
            var response = await GameServer.Tournament.SnapshotAsync(new TournamentEmptyReq(), GameServer.Options());
            ApplyServerSnapshot(response.Data ?? throw new InvalidOperationException("Tournament snapshot is empty."));
        }
        private async UniTask ReloadServerSnapshotWithAlertAsync()
        {
            try { await ReloadServerSnapshotAsync(); }
            catch (Exception error) { PopupManager.instance.AlertShow(error.Message); }
        }
        private TournamentBatchData FromFormation(TournamentFormationInput input, bool attack)
        {
            var result = EmptyTeam(GameServer.Uid);
            if (input == null) return result;
            foreach (var position in input.Heroes)
            {
                var character = m_serverCharacters?.Characters.FirstOrDefault(x => x.CharacterId == position.CharacterId);
                if (character == null) throw new InvalidOperationException("Owned tournament character is missing: " + position.CharacterId);
                var hero = ServerState.ToHero(character);
                hero.sortIdx = position.Position;
                hero.isMain = false;
                hero.isBatch = true;
                hero.isTournament = true;
                hero.isTournament_Attack = attack;
                hero.serverCharacterId = character.CharacterId;
                hero.serverCombatPower = character.CombatPower;
                result.heroes.Add(hero);
            }
            result.treasure = input.TreasureIds.Select((id, index) => new Data_Stat_Relic.TreasureBatchData
            { key = TreasureKey(id), isBatch = true, tickBatch = index + 1 }).ToList();
            return result;
        }
        private static TournamentBatchData FromCombat(TournamentCombatSnapshot snapshot, long uid, bool attack)
        {
            var result = EmptyTeam(uid);
            if (snapshot == null) return result;
            result.serverPower = snapshot.CombatPower;
            foreach (var character in snapshot.Heroes)
            {
                var hero = new HeroInfoData(character.CharacterKey, (GradeType)(int)character.CharacterGrade,
                    _enchantLevel: checked((int)character.GrowthStage), _relicLevel: checked((int)character.RelicEnhanceStage),
                    _isBatch: true, _isMine: uid == GameServer.Uid, _sortIdx: character.Position,
                    _statData: ServerState.ToCombatStats(character.CombatStats));
                hero.serverCharacterId = character.CharacterId;
                hero.serverCombatPower = character.CombatPower;
                hero.isTournament = true;
                hero.isTournament_Attack = attack;
                result.heroes.Add(hero);
            }
            result.treasure = snapshot.Treasures.Select((treasure, index) => new Data_Stat_Relic.TreasureBatchData
            { key = treasure.TreasureKey, isBatch = true, tickBatch = index + 1 }).ToList();
            return result;
        }
        private void ApplyServerSnapshot(TournamentSnapshotDto snapshot)
        {
            if (m_serverSnapshot?.User != null && snapshot.User != null && snapshot.SeasonId == m_serverSnapshot.SeasonId
                && long.TryParse(snapshot.User.Revision, out var incoming) && long.TryParse(m_serverSnapshot.User.Revision, out var previous)
                && incoming < previous) return;
            m_serverSnapshot = snapshot;
            m_data ??= new TournamentData();
            var user = snapshot.User;
            m_data.rankData = new RankerUserData
            {
                uid = checked((int)GameServer.Uid), nickname = DataManager.userInfo.nickname,
                skin = DataManager.userInfo.profileSkin, profileIdx = DataManager.userInfo.profileIdx,
                point = user?.Score ?? 0, rank = checked((int)(user?.Rank ?? 0))
            };
            m_data.rankData.SetTier(checked((int)(user?.TierIdx ?? 8)));
            m_data.grade = GradeType.Normal;
            m_data.countPlay = checked((int)(user?.RemainingCount ?? 0));
            m_data.countAD = checked((int)Math.Max(0, Parameter("TOURNAMENT_DAILY_AD_BATTLE_COUNT", 2) - (user?.AdUsedCount ?? 0)));
            m_data.countRefresh = checked((int)(user?.FreeRefreshCount ?? 0));
            m_data.tick = DateTime.Parse(snapshot.ServerTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime().Ticks;
            m_data.tickRefresh = user?.RechargeAt == null ? m_data.tick : DateTime.Parse(user.RechargeAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                .ToUniversalTime().AddSeconds(Parameter("TOURNAMENT_REFRESH_FREE_RECHARGE_SECONDS", 300)).Ticks;
            m_data.teamAttack = FromFormation(user?.AttackFormation, true);
            m_data.teamDefence = FromCombat(user?.DefenseSnapshot, GameServer.Uid, false);
            m_dbRankUserInfoData.Clear();
            m_dbRankUserInfoData[checked((int)GameServer.Uid)] = m_data.teamDefence;
            m_data.battleUserList = user?.Candidates.Select(candidate => new TournamentRankerUserData
            {
                info = new RankerUserData
                {
                    uid = checked((int)candidate.Uid), nickname = candidate.Nickname, point = candidate.Score,
                    power = candidate.CombatPower, skin = ProfileKey(candidate.ProfileCharacterId)
                },
                // Candidate contract deliberately has no formation. Entry supplies the actual locked defense.
                batchData = CandidatePreview(candidate.Uid, candidate.CombatPower)
            }).ToArray() ?? Array.Empty<TournamentRankerUserData>();
            var ranking = new RankerData { my = m_data.rankData, ranker = new List<RankerUserData>() };
            m_dbRankData[PopupLobbyBossRaid_PopupRanking.TabType.Tutorial_Point] = ranking;
        }
        private static TournamentBatchData CandidatePreview(long uid, long power)
        {
            var preview = EmptyTeam(uid);
            preview.serverPower = power;
            return preview;
        }
        private void ApplyServerMutation(TournamentMutationRes result)
        {
            ApplyServerSnapshot(result.Tournament);
            ServerState.ApplyAsset(result.Asset);
        }
        private TournamentFormationInput ToFormation(TournamentBatchData team) => new TournamentFormationInput
        {
            Heroes = team.heroes.Select(hero => new TournamentHeroPositionDto { CharacterId = CharacterId(hero.key), Position = hero.sortIdx }).ToList(),
            TreasureIds = team.treasure.Where(x => x.isBatch).OrderBy(x => x.tickBatch).Select(x => TreasureId(x.key)).ToList()
        };
        private async UniTask SaveTeamServerAsync(bool attack, TournamentBatchData team)
        {
            var input = ToFormation(team);
            if (input.Heroes.Count == 0) throw new InvalidOperationException("Select at least one owned hero.");
            var key = (attack ? "attack:" : "defense:") + JsonConvert.SerializeObject(input);
            await m_serverActionGate.WaitAsync();
            try
            {
                var response = attack
                    ? await GameServer.Tournament.SetAttackAsync(input, PendingOptions(key))
                    : await GameServer.Tournament.SetDefenseAsync(input, PendingOptions(key));
                ApplyServerMutation(response.Data ?? throw new InvalidOperationException("Tournament formation response is empty."));
                m_pendingServerRequests.Remove(key);
            }
            finally { m_serverActionGate.Release(); }
        }
        public async UniTask<bool> SetTreasureServerAsync(string key, bool equipped)
        {
            var team = GetBatchData(isAttackType);
            team.treasure.RemoveAll(x => x.key == key);
            if (equipped) team.treasure.Add(new Data_Stat_Relic.TreasureBatchData { key = key, isBatch = true, tickBatch = Utils.GetUTC().Ticks });
            try { await SaveTeamServerAsync(isAttackType, team); return true; }
            catch (Exception error) { PopupManager.instance.AlertShow(error.Message); return false; }
        }
        private async UniTask<bool> CompleteAdServerAsync()
        {
            await m_serverActionGate.WaitAsync();
            const string key = "ad";
            try
            {
                if (!m_pendingServerRequests.ContainsKey(key) && !await AdsManager.instance.ShowAsync()) return false;
                var response = await GameServer.Tournament.CompleteAdAsync(new TournamentEmptyReq(), PendingOptions(key));
                ApplyServerMutation(response.Data ?? throw new InvalidOperationException("Tournament ad response is empty."));
                m_pendingServerRequests.Remove(key);
                return true;
            }
            catch (Exception error) { PopupManager.instance.AlertShow(error.Message); return false; }
            finally { m_serverActionGate.Release(); }
        }
        private async UniTask RefreshServerListAsync()
        {
            await m_serverActionGate.WaitAsync();
            try
            {
                if (!ServerAvailable) throw new InvalidOperationException("Tournament is preparing.");
                var request = new TournamentRefreshReq { CandidateSetRevision = m_serverSnapshot.User.CandidateSetRevision };
                var key = "refresh:" + request.CandidateSetRevision;
                var response = await GameServer.Tournament.RefreshAsync(request, PendingOptions(key));
                ApplyServerMutation(response.Data ?? throw new InvalidOperationException("Tournament refresh response is empty."));
                m_pendingServerRequests.Remove(key);
                if (m_data.countRefresh < Parameter("TOURNAMENT_REFRESH_FREE_LIMIT_COUNT", 3)) TimerServerRefreshAsync().Forget();
            }
            catch (Exception error) { PopupManager.instance.AlertShow(error.Message); }
            finally { m_serverActionGate.Release(); }
        }
        private async UniTask TimerServerRefreshAsync()
        {
            m_ctsRefresh = m_ctsRefresh.ReleaseCTS(true);
            var token = m_ctsRefresh.Token;
            try
            {
                while (m_data.countRefresh < Parameter("TOURNAMENT_REFRESH_FREE_LIMIT_COUNT", 3))
                {
                    var seconds = Math.Max(1, (new DateTime(m_data.tickRefresh, DateTimeKind.Utc) - Utils.GetUTC()).TotalSeconds);
                    await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
                    await ReloadServerSnapshotAsync();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { PopupManager.instance.AlertShow(error.Message); }
        }
        private async UniTask<bool> EnterServerBattleAsync(int uid, int historyIndex)
        {
            if (m_enteringServer || isRunning) return false;
            m_enteringServer = true;
            await m_serverActionGate.WaitAsync();
            try
            {
                if (!ServerAvailable) throw new InvalidOperationException("Tournament is preparing.");
                TournamentEnterRes result;
                string key;
                if (historyIndex >= 0)
                {
                    if (!m_serverHistoryIds.TryGetValue(historyIndex, out var battleId)) throw new InvalidOperationException("Reload tournament history first.");
                    key = "revenge:" + battleId;
                    var response = await GameServer.Tournament.RevengeAsync(new TournamentRevengeReq { SourceBattleId = battleId }, PendingOptions(key));
                    result = response.Data;
                }
                else
                {
                    var candidate = m_serverSnapshot.User.Candidates.FirstOrDefault(x => x.Uid == uid)
                        ?? throw new InvalidOperationException("Reload tournament candidates first.");
                    key = "start:" + m_serverSnapshot.User.CandidateSetRevision + ":" + candidate.Slot;
                    var response = await GameServer.Tournament.StartAsync(new TournamentStartReq
                    { CandidateSetRevision = m_serverSnapshot.User.CandidateSetRevision, Slot = candidate.Slot }, PendingOptions(key));
                    result = response.Data;
                }
                if (result == null) throw new InvalidOperationException("Tournament entry response is empty.");
                m_serverPreviousRank = m_data.rankData.DeepClone();
                var previousInfo = m_data.battleUserList.FirstOrDefault(x => x.info.uid == uid)?.info;
                ApplyServerSnapshot(result.Tournament);
                ServerState.ApplyAsset(result.Asset);
                m_serverEntry = result.Entry;
                m_serverResult = null;
                m_data.teamAttack = FromCombat(m_serverEntry.Attack, GameServer.Uid, true);
                enterUserData = new TournamentRankerUserData
                {
                    info = previousInfo ?? new RankerUserData { uid = uid, nickname = "UID " + uid, power = m_serverEntry.Defense.CombatPower },
                    batchData = FromCombat(m_serverEntry.Defense, m_serverEntry.TargetUid, false)
                };
                m_dbRankUserInfoData[uid] = enterUserData.batchData;
                m_pendingServerRequests.Remove(key);
                return true;
            }
            catch (Exception error) { PopupManager.instance.AlertShow(error.Message); return false; }
            finally { m_enteringServer = false; m_serverActionGate.Release(); }
        }
        private async UniTask<RankerUserData> FinishServerBattleAsync()
        {
            if (m_serverResult != null) return m_data.rankData;
            if (m_serverEntry == null) throw new InvalidOperationException("Tournament has no active server battle.");
            var request = new TournamentFinishReq
            { BattleId = m_serverEntry.BattleId, Result = TournamentHeroInfoManager.instance.IsWin() ? TournamentBattleResult.Win : TournamentBattleResult.Lose };
            var key = "finish:" + request.BattleId + ":" + request.Result;
            var response = await GameServer.Tournament.FinishAsync(request, PendingOptions(key));
            var result = response.Data ?? throw new InvalidOperationException("Tournament result response is empty.");
            ApplyServerSnapshot(result.Tournament);
            ServerState.ApplyAsset(result.Asset);
            m_serverResult = result;
            m_pendingServerRequests.Remove(key);
            return m_data.rankData;
        }
        private async UniTask FinishAndShowServerAsync()
        {
            if (m_serverFinishing || statusType == TournamentStatusType.Finished) return;
            m_serverFinishing = true;
            statusType = TournamentStatusType.Finished;
            Signal.instance.TournamentStatus.Emit(TournamentStatusType.Finished);
            try
            {
                while (true)
                {
                    try { await FinishServerBattleAsync(); break; }
                    catch (Exception error)
                    {
                        PopupManager.instance.AlertShow(error.Message);
                        if (await PopupManager.instance.OpenModalAsync("Server result failed. Retry settlement?") != StatusType.Success)
                        { await ExitAsync(); return; }
                    }
                }
                PopupManager.instance.CloseAll();
                PopupManager.instance.OpenPopup(PopupType.TournamentResult);
            }
            finally { m_serverFinishing = false; }
        }
        private async UniTask<RankerData> LoadServerRankingAsync(PopupLobbyBossRaid_PopupRanking.TabType tab)
        {
            if (m_serverSnapshot == null) await ReloadServerSnapshotAsync();
            var ranking = new RankerData { ranker = new List<RankerUserData>(), my = m_data.rankData };
            string cursor = null;
            do
            {
                var response = await GameServer.Tournament.RankingAsync(new TournamentRankingReq
                { SeasonId = m_serverSnapshot.SeasonId, Cursor = cursor, Limit = 100 }, GameServer.Options());
                var result = response.Data ?? throw new InvalidOperationException("Tournament ranking response is empty.");
                foreach (var row in result.Entries)
                {
                    var user = new RankerUserData
                    { uid = checked((int)row.Uid), nickname = row.Nickname, rank = checked((int)row.Rank), point = row.Score, skin = ProfileKey(row.ProfileCharacterId) };
                    user.SetTier(checked((int)row.TierIdx));
                    ranking.ranker.Add(user);
                    if (row.Uid == GameServer.Uid) ranking.my = user;
                }
                ranking.my.rank = checked((int)(result.MyRank ?? 0));
                cursor = result.NextCursor;
            } while (cursor != null && ranking.ranker.Count < 500);
            m_data.rankData = ranking.my;
            m_dbRankData[tab] = ranking;
            m_dbRankData[PopupLobbyBossRaid_PopupRanking.TabType.Tutorial_Point] = ranking;
            return ranking;
        }
        private async UniTask<List<TournamentHistoryData>> LoadServerHistoryAsync()
        {
            var history = new List<TournamentHistoryData>();
            m_serverHistoryIds.Clear();
            string cursor = null;
            do
            {
                var response = await GameServer.Tournament.HistoryAsync(new TournamentHistoryReq { Cursor = cursor, Limit = 50 }, GameServer.Options());
                var result = response.Data ?? throw new InvalidOperationException("Tournament history response is empty.");
                foreach (var row in result.Battles.Where(x => x.Status == TournamentBattleStatus.Completed))
                {
                    var attack = row.AttackerUid == GameServer.Uid;
                    var uid = attack ? row.DefenderUid : row.AttackerUid;
                    var candidate = m_data.battleUserList.FirstOrDefault(x => x.info.uid == uid);
                    var index = history.Count + 1;
                    m_serverHistoryIds[index] = row.BattleId;
                    history.Add(new TournamentHistoryData
                    {
                        index = index, uid = checked((int)uid), nickname = candidate?.info.nickname ?? "UID " + uid,
                        skin = candidate?.info.skin, isAttack = attack, isWin = row.Result == TournamentBattleResult.Win,
                        resultPoint = checked((int)row.ScoreDelta), batchData = EmptyTeam(uid),
                        tick = row.CompletedAt == null ? 0 : DateTime.Parse(row.CompletedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime().Ticks,
                        serverRevengeAvailable = row.RevengeAvailable,
                        serverRevengeEnd = row.CompletedAt == null ? DateTime.MinValue : DateTime.Parse(row.CompletedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                            .ToUniversalTime().AddSeconds(Parameter("TOURNAMENT_REVENGE_EXPIRE_SECONDS", 86400))
                    });
                }
                cursor = result.NextCursor;
            } while (cursor != null && history.Count < 100);
            history.Reverse();
            return m_history = history;
        }
    }
}
