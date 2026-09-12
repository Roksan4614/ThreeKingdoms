#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rev9.ContentsMarket;
using Rev9.Tournament;
using ThreeKingdoms.Shared.Enums;
using ThreeKingdoms.Shared.Types;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThreeKingdoms.Client.Server
{
    // Explicit proof requests run through the currently logged-in Unity session. This never logs in or starts Play Mode.
    public static class ServerIntegrationProof
    {
        [Serializable]
        public sealed class Request
        {
            public string request_id;
            public string operation = "snapshot";
            public string[] steps = Array.Empty<string>();
            public bool screenshot;
        }

        private sealed class Blocked : Exception
        {
            public readonly JObject Evidence;
            public Blocked(string reason, JObject evidence = null) : base(reason) { Evidence = evidence; }
        }

        private static readonly string[] SnapshotSteps = { "assets", "characters", "castle", "shop", "daily", "tournament", "raid" };
        private static readonly Regex RequestLog = new Regex(@"^\[SERVER_API\] uid=(\d+) path=(\S+) code=(\S+) request_id=(\S+)");

        public static async UniTask<JObject> RunAsync(Request request, string outputDirectory, CancellationToken token)
        {
            var result = new JObject
            {
                ["request_id"] = request.request_id, ["operation"] = request.operation, ["started_at"] = DateTime.UtcNow.ToString("O"),
                ["status"] = "running", ["evidence_boundary"] = "Actual Unity PlayMode session and API; persisted changes belong to dedicated DEV UID 29."
            };
            result["request_attribution"] = "Observed same-UID Unity traffic during each step; background refresh requests may also be included.";
            var steps = new JArray();
            result["steps"] = steps;
            var requests = new List<JObject>();
            void Capture(string message, string stack, LogType type)
            {
                var match = RequestLog.Match(message);
                if (match.Success && match.Groups[1].Value == "29") requests.Add(new JObject
                {
                    ["path"] = match.Groups[2].Value, ["code"] = match.Groups[3].Value, ["request_id"] = match.Groups[4].Value
                });
            }
            Application.logMessageReceived += Capture;
            try
            {
                EnsureSession();
                if (request.operation != "snapshot" && request.operation != "exercise") throw new Blocked("OPERATION_MUST_BE_SNAPSHOT_OR_EXERCISE");
                var names = request.operation == "snapshot" ? SnapshotSteps : request.steps;
                if (names == null || names.Length == 0) throw new Blocked("EXERCISE_REQUIRES_EXPLICIT_STEPS");
                if (names.Distinct().Count() != names.Length) throw new Blocked("DUPLICATE_STEPS_NOT_ALLOWED");
                result["uid"] = GameServer.Uid;
                result["table_hash"] = GameServer.TableVersion;
                result["api_base_url"] = new Uri(GameServer.Settings.ApiBaseUrl).GetLeftPart(UriPartial.Authority);
                result["before"] = CaptureState();
                foreach (var name in names)
                {
                    token.ThrowIfCancellationRequested();
                    EnsureSession();
                    var record = new JObject { ["name"] = name, ["status"] = "running", ["started_at"] = DateTime.UtcNow.ToString("O") };
                    steps.Add(record);
                    WriteJson(Path.Combine(outputDirectory, "progress.json"), result);
                    var firstRequest = requests.Count;
                    var timer = Stopwatch.StartNew();
                    try
                    {
                        var details = request.operation == "snapshot" ? await SnapshotAsync(name) : await ExerciseAsync(name, token);
                        record["details"] = details;
                        record["status"] = details["checks"] is JArray checks && checks.Any(check => (bool?)check["passed"] == false) ? "failed" : "passed";
                    }
                    catch (Blocked error)
                    {
                        record["status"] = "blocked"; record["reason"] = error.Message;
                        if (error.Evidence != null) record["details"] = error.Evidence;
                    }
                    catch (GameServerException error)
                    {
                        record["status"] = error.HttpStatus > 0 && error.HttpStatus < 500 ? "blocked" : "failed";
                        record["reason"] = error.Code; record["http_status"] = error.HttpStatus; record["request_id"] = error.RequestId;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception error) { record["status"] = "failed"; record["reason"] = Sanitize(error.GetType().Name + ": " + error.Message); }
                    record["elapsed_ms"] = timer.ElapsedMilliseconds;
                    record["observed_requests"] = new JArray(requests.Skip(firstRequest));
                    record["after"] = CaptureState();
                    WriteJson(Path.Combine(outputDirectory, "progress.json"), result);
                }
                result["status"] = steps.Any(step => (string)step["status"] == "failed") ? "failed" : steps.Any(step => (string)step["status"] == "blocked") ? "blocked" : "passed";
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
                result["after"] = CaptureState();
                result["scene"] = CaptureScene();
                if (request.screenshot)
                {
                    var screenshot = Path.Combine(outputDirectory, "screenshot-" + request.request_id + ".png");
                    ScreenCapture.CaptureScreenshot(screenshot);
                    for (var attempt = 0; attempt < 30 && !File.Exists(screenshot); attempt++) await UniTask.Delay(100, ignoreTimeScale: true, cancellationToken: token);
                    result["screenshot"] = new JObject { ["path"] = screenshot, ["file_exists"] = File.Exists(screenshot) };
                }
            }
            catch (Blocked error) { result["status"] = "blocked"; result["reason"] = error.Message; }
            catch (OperationCanceledException) { result["status"] = "failed"; result["reason"] = "PLAYMODE_OR_REQUEST_CANCELLED"; }
            catch (Exception error) { result["status"] = "failed"; result["reason"] = Sanitize(error.GetType().Name + ": " + error.Message); }
            finally
            {
                Application.logMessageReceived -= Capture;
                result["completed_at"] = DateTime.UtcNow.ToString("O");
            }
            return result;
        }

        private static void EnsureSession()
        {
            if (!Application.isPlaying) throw new Blocked("PLAYMODE_REQUIRED");
            if (!GameServer.Enabled || !GameServer.IsLoggedIn) throw new Blocked("CURRENT_UNITY_LOGIN_REQUIRED");
            if (GameServer.Uid != 29 || GameServer.Settings.TestUid != 29) throw new Blocked("DEDICATED_TEST_UID_29_REQUIRED");
            var api = new Uri(GameServer.Settings.ApiBaseUrl);
            if (!api.IsLoopback || api.Port != 11080) throw new Blocked("EXISTING_LOCAL_API_11080_REQUIRED");
        }

        private static void RegionRequired()
        {
            if (GameServer.Login?.RegionSelected != true) throw new Blocked("REGION_NOT_SELECTED");
        }

        private static JObject Check(string name, object expected, object actual) => new JObject
        {
            ["name"] = name, ["expected"] = Json(expected), ["actual"] = Json(actual), ["passed"] = JToken.DeepEquals(Json(expected), Json(actual))
        };
        private static JToken Json(object value) => value == null ? JValue.CreateNull() : JToken.FromObject(value);
        private static JObject Details(object value, params JObject[] checks) => new JObject { ["observed"] = Json(value), ["checks"] = new JArray(checks) };

        private static async UniTask<JObject> SnapshotAsync(string name)
        {
            if (name == "assets")
            {
                var response = (await GameServer.Currency.InfoAsync(new EmptyRes(), GameServer.Options())).Data;
                return Details(response,
                    Check("cache_asset", response, ServerState.Asset),
                    Check("view_gold", response.FreeGold + response.PaidGold, DataManager.userInfo.GetAssetAmount(ItemType.gold)),
                    Check("view_rice", response.Rice, DataManager.userInfo.GetAssetAmount(ItemType.rice)));
            }
            RegionRequired();
            switch (name)
            {
                case "characters":
                    await CharacterActions.RefreshAsync();
                    return CharacterEvidence();
                case "castle":
                    await DataManager.castle.RefreshServerAsync();
                    return CastleEvidence();
                case "shop":
                    var shops = (await GameServer.ContentShop.LobbyAsync(new ContentShopLobbyReq(), GameServer.Options())).Data;
                    await ContentsMarketWorker.instance.InitializeAsync();
                    var shopChecks = new List<JObject>();
                    foreach (var shop in shops.Shops)
                    {
                        var local = ContentsMarketWorker.instance.GetProducts(ShopTab(shop.ShopType));
                        shopChecks.Add(Check(shop.ShopType + "_product_ids", shop.Products.Select(row => row.ProductId).OrderBy(id => id).ToArray(), local.Select(row => (long)row.idx).OrderBy(id => id).ToArray()));
                        foreach (var item in shop.Products) shopChecks.Add(Check("bought_" + item.ProductId, item.BoughtCount, (long)local.Single(row => row.idx == item.ProductId).countBuy));
                    }
                    return Details(shops, shopChecks.ToArray());
                case "daily":
                    var daily = (await GameServer.DailyDungeon.LobbyAsync(new DailyDungeonLobbyReq(), GameServer.Options())).Data;
                    await DataManager.dailyDungeon.RefreshServerAsync();
                    return Details(daily, Check("remaining_count", daily.RemainingCount, (long)DataManager.dailyDungeon.data.count));
                case "tournament":
                    var tournament = (await GameServer.Tournament.SnapshotAsync(new TournamentEmptyReq(), GameServer.Options())).Data;
                    await TournamentWorker.instance.InitailizeAsync();
                    return Details(tournament, Check("available", tournament.SeasonStatus == TournamentAvailabilityStatus.Open && tournament.User != null, TournamentWorker.instance.ServerAvailable));
                case "raid":
                    await DataManager.bossRaid.RefreshServerAsync();
                    var raid = DataManager.bossRaid.ServerRound;
                    return Details(DataManager.bossRaid.ServerRaid,
                        Check("boss_key", raid?.BossKey ?? "LuBu", DataManager.bossRaid.data.keyBoss),
                        Check("table_hash", GameServer.TableVersion, raid?.TableVersion ?? GameServer.TableVersion));
                default: throw new Blocked("UNKNOWN_SNAPSHOT_STEP:" + name);
            }
        }

        private static JObject CharacterEvidence()
        {
            var snapshot = ServerState.Characters;
            var checks = new List<JObject> { Check("table_hash", GameServer.TableVersion, snapshot.TableVersion) };
            foreach (var character in snapshot.Characters)
            {
                var view = DataManager.userInfo.myHero.SingleOrDefault(hero => hero.key == character.CharacterKey);
                checks.Add(Check(character.CharacterKey + "_growth", character.GrowthStage, view == null ? -1L : (long)view.enchantLevel));
                checks.Add(Check(character.CharacterKey + "_grade", (int)character.CharacterGrade, view == null ? -1 : (int)view.grade));
            }
            foreach (var item in snapshot.Items)
                checks.Add(Check("inventory_" + item.ItemId, item.Amount, InventoryWorker.instance.GetItemCount(ServerState.ToItem(item.ItemId, 1))));
            return Details(snapshot, checks.ToArray());
        }

        private static JObject CastleEvidence()
        {
            var castle = DataManager.castle;
            var snapshot = castle.ServerCastle;
            var checks = new List<JObject> { Check("table_hash", GameServer.TableVersion, snapshot.TableVersion) };
            foreach (var row in snapshot.Buildings)
            {
                var type = (CastleObjectType)Enum.Parse(typeof(CastleObjectType), row.BuildingKey, true);
                var view = castle.GetCaslteData(type);
                checks.Add(Check(row.BuildingKey + "_level", row.Level, (long)view.level));
                checks.Add(Check(row.BuildingKey + "_assignment", row.AssignedCharacterIds.ToArray(), view.heroes.Select(ServerState.CharacterId).ToArray()));
                if (row.Production != null)
                {
                    var amount = double.Parse(row.Production.ProductionAmount, CultureInfo.InvariantCulture);
                    checks.Add(Check(row.BuildingKey + "_production_float_display", true, Math.Abs(view.totalAmount - amount) <= Math.Max(.01, Math.Abs(amount) * 0.000001)));
                }
            }
            var office = castle.mission.ServerOffice;
            checks.Add(Check("office_remaining_count", office.RemainingCount, (long)castle.mission.levelInfo.missionCount));
            checks.Add(Check("office_offer_ids", office.Offers.Select(row => row.OfferId).OrderBy(id => id).ToArray(), castle.mission.GetMissions(false).Select(row => row.serverOfferId).OrderBy(id => id).ToArray()));
            var upgradeEligibility = snapshot.Buildings.Select(row =>
            {
                var type = (CastleObjectType)Enum.Parse(typeof(CastleObjectType), row.BuildingKey, true);
                return new { building_key = row.BuildingKey, current_level = row.Level, manual_upgrade = type != CastleObjectType.Office,
                    can_start = castle.CanServerUpgrade(type), prerequisite_met = castle.ServerUpgradePrerequisitesMet(type), next_requirements = castle.ServerUpgradeRequirements(type) };
            }).ToArray();
            return Details(new { castle = snapshot, upgrade = castle.ServerUpgrade, office, last_assignment = castle.LastServerAssignment, upgrade_eligibility = upgradeEligibility }, checks.ToArray());
        }

        private static ContentsMarketTabType ShopTab(ContentShopType type) => type switch
        {
            ContentShopType.Daily => ContentsMarketTabType.Daily, ContentShopType.Raid => ContentsMarketTabType.Raid,
            ContentShopType.Tournament => ContentsMarketTabType.Tournament, _ => throw new Blocked("UNSUPPORTED_SHOP")
        };

        private static async UniTask<JObject> ExerciseAsync(string name, CancellationToken token)
        {
#if UNITY_EDITOR
            if (name == "move_touch") return await ServerInputProof.MoveTouchAsync(token);
            if (name == "attack_touch" || name == "skill_touch" || name == "dash_touch")
                return await ServerInputProof.ButtonTouchAsync(name.Substring(0, name.IndexOf('_')), token);
#endif
            if (name == "capture_input") return ServerInputProof.Capture();
            if (name == "prepare_mobile_controls")
            {
#if UNITY_EDITOR
                return ServerInputProof.PrepareMobileControls();
#else
                throw new Blocked("MOBILE_CONTROL_FIXTURE_IS_EDITOR_ONLY");
#endif
            }
            if (SnapshotSteps.Contains(name)) return await SnapshotAsync(name);
            if (ServerCharacterProof.Handles(name))
            {
                var characterResult = await ServerCharacterProof.RunAsync(name, token);
                if ((string)characterResult["status"] == "blocked")
                    throw new Blocked((string)characterResult["reason"], characterResult);
                return characterResult;
            }
            RegionRequired();
            if (ServerState.Characters == null) await CharacterActions.RefreshAsync();
            var hero = ServerState.Characters.Characters.FirstOrDefault();
            switch (name)
            {
                case "grow":
                    if (hero == null) throw new Blocked("NO_OWNED_CHARACTER");
                    var grown = await CharacterActions.GrowAsync(hero.CharacterKey);
                    var growthEvidence = CharacterEvidence();
                    growthEvidence["event_id"] = grown.EventId;
                    growthEvidence["growth_result"] = grown.GrowthResult.ToString();
                    return growthEvidence;
                case "open_item":
                    await CharacterActions.RefreshAsync();
                    var container = ServerState.Characters.Items.FirstOrDefault(item => item.Amount > 0 && GameServer.TableRows("s_item").Any(row => (long)row["idx"] == item.ItemId && (string)row["key"] == "rice_pocket_normal"));
                    if (container == null) throw new Blocked("NO_RICE_POCKET_NORMAL");
                    var opened = (await GameServer.Item.OpenContainerAsync(new OpenItemContainerReq { ItemId = container.ItemId, Quantity = 1 }, GameServer.Options())).Data;
                    ServerState.ApplyAsset(opened.Asset); ServerState.ApplyItems(opened.ItemUpdates);
                    if (opened.CharacterSnapshot != null) await ServerState.ApplyCharactersAsync(opened.CharacterSnapshot);
                    await CharacterActions.RefreshAsync();
                    return Details(opened, Check("view_rice", opened.Asset.Rice, DataManager.userInfo.GetAssetAmount(ItemType.rice)));
                case "buy_shop":
                    var catalog = (await GameServer.ContentShop.LobbyAsync(new ContentShopLobbyReq(), GameServer.Options())).Data;
                    var daily = catalog.Shops.SingleOrDefault(shop => shop.ShopType == ContentShopType.Daily && shop.PurchaseEnabled);
                    var product = daily?.Products.FirstOrDefault(item => !item.RemainingCount.HasValue || item.RemainingCount > 0);
                    if (product == null) throw new Blocked("NO_AVAILABLE_DAILY_SHOP_PRODUCT", Details(catalog));
                    await ContentsMarketWorker.instance.InitializeAsync();
                    var uiProduct = ContentsMarketWorker.instance.GetProducts(ContentsMarketTabType.Daily).Single(item => item.idx == product.ProductId);
                    if (!await ContentsMarketWorker.instance.API_ProductBuy(ContentsMarketTabType.Daily, uiProduct, 1)) throw new Blocked("PURCHASE_REJECTED:" + GameServer.LastError);
                    var afterProduct = ContentsMarketWorker.instance.GetProducts(ContentsMarketTabType.Daily).Single(item => item.idx == product.ProductId);
                    return Details(afterProduct, Check("purchase_count", product.BoughtCount + 1, (long)afterProduct.countBuy));
                case "castle_assign":
                case "castle_assign_palace":
                    if (hero == null) throw new Blocked("NO_OWNED_CHARACTER");
                    await DataManager.castle.RefreshServerAsync();
                    var assignmentType = name == "castle_assign_palace" ? CastleObjectType.Palace : CastleObjectType.Farm;
                    var assignment = DataManager.castle.GetCaslteData(assignmentType).DeepClone();
                    assignment.heroes = new List<string> { hero.CharacterKey };
                    var assigned = StatusType.Wait;
                    await DataManager.castle.SetBatchHeroAsync(assignment, status => assigned = status);
                    if (assigned != StatusType.Success) throw new Blocked("ASSIGNMENT_REJECTED:" + GameServer.LastError, CastleEvidence());
                    return CastleEvidence();
                case "castle_collect":
                    await DataManager.castle.RefreshServerAsync();
                    var produced = DataManager.castle.ServerCastle.Buildings.Where(row => row.Production != null).OrderByDescending(row => double.Parse(row.Production.ProductionAmount, CultureInfo.InvariantCulture)).FirstOrDefault();
                    if (produced == null || double.Parse(produced.Production.ProductionAmount, CultureInfo.InvariantCulture) < 1) throw new Blocked("NO_PRODUCTION_TO_COLLECT", CastleEvidence());
                    var collected = StatusType.Wait;
                    await DataManager.castle.ClaimAsync((CastleObjectType)Enum.Parse(typeof(CastleObjectType), produced.BuildingKey, true), status => collected = status);
                    if (collected != StatusType.Success) throw new Blocked("COLLECT_REJECTED:" + GameServer.LastError);
                    return CastleEvidence();
                case "castle_upgrade":
                    await DataManager.castle.RefreshServerAsync();
                    var eligible = DataManager.castle.ServerCastle.Buildings.Where(row => row.BuildingKey != "office").Select(row => (CastleObjectType)Enum.Parse(typeof(CastleObjectType), row.BuildingKey, true))
                        .FirstOrDefault(type => !DataManager.castle.GetCaslteData(type).isDoingUpgrade && DataManager.castle.CanServerUpgrade(type));
                    if (!DataManager.castle.CanServerUpgrade(eligible) || DataManager.castle.GetCaslteData(eligible).isDoingUpgrade) throw new Blocked("NO_UPGRADE_PREREQUISITES", CastleEvidence());
                    var upgradeStarted = false;
                    await DataManager.castle.building.StartUpgradeAsync(eligible, _ => upgradeStarted = true);
                    if (!upgradeStarted) throw new Blocked("UPGRADE_REJECTED:" + GameServer.LastError, CastleEvidence());
                    return CastleEvidence();
                case "office_start":
                    if (hero == null) throw new Blocked("NO_OWNED_CHARACTER");
                    await DataManager.castle.RefreshServerAsync();
                    var offers = DataManager.castle.mission.GetMissions(false);
                    foreach (var offer in offers) offer.heroes = new List<string> { hero.CharacterKey };
                    var selected = offers.OrderByDescending(offer => DataManager.castle.mission.GetTotalCoreStat(offer) / (double)Math.Max(1, offer.coreStatMax)).FirstOrDefault();
                    if (selected == null) throw new Blocked("NO_OFFICE_OFFERS");
                    var started = StatusType.Wait;
                    await DataManager.castle.mission.StartMissionAsync(selected, status => started = status);
                    if (started != StatusType.Success) throw new Blocked("OFFICE_START_REJECTED:" + GameServer.LastError, CastleEvidence());
                    return CastleEvidence();
                case "office_claim":
                    await DataManager.castle.RefreshServerAsync();
                    var finished = DataManager.castle.mission.GetFinishedMissions();
                    if (finished.Length == 0) throw new Blocked("NO_COMPLETED_OFFICE_RUN", CastleEvidence());
                    var claimed = StatusType.Wait;
                    var rewards = await DataManager.castle.mission.CompleteMissionAsync((status, _) => claimed = status, finished[0]);
                    if (claimed != StatusType.Success) throw new Blocked("OFFICE_CLAIM_REJECTED:" + GameServer.LastError);
                    return Details(new { rewards, office = DataManager.castle.mission.ServerOffice });
                case "daily_enter":
                    var lobby = (await GameServer.DailyDungeon.LobbyAsync(new DailyDungeonLobbyReq(), GameServer.Options())).Data;
                    if (lobby.ActiveEntry != null) throw new Blocked("DAILY_ENTRY_ALREADY_ACTIVE", Details(lobby));
                    if (lobby.RemainingCount <= 0 || lobby.AvailableBossIds.Count == 0) throw new Blocked("DAILY_ENTRY_UNAVAILABLE", Details(lobby));
                    var boss = GameServer.TableRows("s_daily_dungeon_boss").Single(row => (long)row["idx"] == lobby.AvailableBossIds[0]);
                    if (!await DataManager.dailyDungeon.EnterAsync((WeekdayType)(int)boss["weekday"])) throw new Blocked("DAILY_ENTER_REJECTED:" + GameServer.LastError);
                    await WaitForAsync(() => SceneManager.GetActiveScene().name.IndexOf("DailyDungeon", StringComparison.OrdinalIgnoreCase) >= 0, token);
                    var entry = (await GameServer.DailyDungeon.LobbyAsync(new DailyDungeonLobbyReq(), GameServer.Options())).Data;
                    return Details(entry, Check("active_entry", true, entry.ActiveEntry != null), Check("view_running", true, DataManager.dailyDungeon.isRunning));
                case "daily_settle":
                    if (!DataManager.dailyDungeon.isRunning || SceneManager.GetActiveScene().name.IndexOf("DailyDungeon", StringComparison.OrdinalIgnoreCase) < 0) throw new Blocked("ACTUAL_DAILY_PLAY_SCENE_REQUIRED");
                    var before = (await GameServer.DailyDungeon.LobbyAsync(new DailyDungeonLobbyReq(), GameServer.Options())).Data;
                    if (before.ActiveEntry == null) throw new Blocked("NO_ACTIVE_DAILY_ENTRY");
                    // This uses the game's current measured HP/grade. No victory, damage, or reward is invented here.
                    DataManager.dailyDungeon.TimeoutAsync().Forget();
                    DailyDungeonLobbyDto settled = null;
                    for (var attempt = 0; attempt < 20; attempt++)
                    {
                        await UniTask.Delay(500, ignoreTimeScale: true, cancellationToken: token);
                        settled = (await GameServer.DailyDungeon.LobbyAsync(new DailyDungeonLobbyReq(), GameServer.Options())).Data;
                        if (settled.ActiveEntry == null) break;
                    }
                    return Details(new { entry_id = before.ActiveEntry.EntryEventId, daily = settled, current_view = DataManager.dailyDungeon.data }, Check("server_entry_settled", null, settled?.ActiveEntry));
                case "tournament_set_attack":
                case "tournament_set_defense":
                    if (hero == null) throw new Blocked("NO_OWNED_CHARACTER");
                    var formation = new TournamentFormationInput { Heroes = new List<TournamentHeroPositionDto> { new TournamentHeroPositionDto { CharacterId = hero.CharacterId, Position = 4 } }, TreasureIds = new List<long>() };
                    if (name == "tournament_set_attack") await GameServer.Tournament.SetAttackAsync(formation, GameServer.Options());
                    else await GameServer.Tournament.SetDefenseAsync(formation, GameServer.Options());
                    await TournamentWorker.instance.InitailizeAsync();
                    var saved = (await GameServer.Tournament.SnapshotAsync(new TournamentEmptyReq(), GameServer.Options())).Data;
                    var formationView = TournamentWorker.instance.GetBatchData(name == "tournament_set_attack").heroes.FirstOrDefault();
                    return Details(saved, Check("view_formation_hero", hero.CharacterKey, formationView?.key), Check("view_formation_position", 4, formationView?.sortIdx));
                case "tournament_refresh":
                    await TournamentWorker.instance.InitailizeAsync();
                    var beforeRefresh = (await GameServer.Tournament.SnapshotAsync(new TournamentEmptyReq(), GameServer.Options())).Data;
                    if (beforeRefresh.User == null) throw new Blocked("TOURNAMENT_NOT_AVAILABLE", Details(beforeRefresh));
                    await TournamentWorker.instance.RefreshListAsync();
                    var refreshed = (await GameServer.Tournament.SnapshotAsync(new TournamentEmptyReq(), GameServer.Options())).Data;
                    if (refreshed.User?.CandidateSetRevision == beforeRefresh.User.CandidateSetRevision) throw new Blocked("TOURNAMENT_REFRESH_NOT_APPLIED:" + GameServer.LastError, Details(refreshed));
                    return Details(refreshed, Check("candidate_revision_changed", true, refreshed.User?.CandidateSetRevision != beforeRefresh.User.CandidateSetRevision));
                case "tournament_ranking":
                    var current = (await GameServer.Tournament.SnapshotAsync(new TournamentEmptyReq(), GameServer.Options())).Data;
                    return Details((await GameServer.Tournament.RankingAsync(new TournamentRankingReq { SeasonId = current.SeasonId, Limit = 20 }, GameServer.Options())).Data);
                case "tournament_enter": return await EnterTournamentProofAsync(token);
                case "tournament_settle": return await SettleTournamentProofAsync(token);
                case "raid_enter": return await EnterRaidProofAsync(token);
                case "raid_snapshot": return await RaidSnapshotProofAsync();
                case "raid_transport":
                    if (DataManager.bossRaid.ServerJoined) throw new Blocked("LEAVE_RAID_BEFORE_TRANSPORT_CHECK");
                    var previousRaid = DataManager.bossRaid.ServerRaid?.LastParticipatedRaidId;
                    if (string.IsNullOrEmpty(previousRaid)) throw new Blocked("PREVIOUS_PARTICIPATED_RAID_REQUIRED");
                    using (var transport = new RaidSocketClient())
                    {
                        JObject JoinPrevious() => new JObject { ["type"] = "RAID_JOIN", ["request_id"] = Guid.NewGuid().ToString(), ["raid_id"] = previousRaid };
                        var first = await transport.RequestAsync<RaidStateRes>(JoinPrevious(), token);
                        var firstGeneration = transport.Generation;
                        await transport.CloseAsync(token);
                        var restored = await transport.RequestAsync<RaidStateRes>(JoinPrevious(), token);
                        return Details(new { before = first, after = restored, first_generation = firstGeneration, restored_generation = transport.Generation,
                            scope = "Actual Unity native socket close/reconnect and existing finished-round reads; no damage or outcome submitted." },
                            Check("same_raid_after_close", first.Round.RaidId, restored.Round.RaidId),
                            Check("new_authenticated_generation", true, transport.Generation > firstGeneration),
                            Check("preserved_raid_points", first.RaidPointBalance, restored.RaidPointBalance));
                    }
                case "raid_reconnect": return await RaidReconnectProofAsync(token);
                case "raid_combat_input": return await RaidCombatInputProofAsync(token);
                default: throw new Blocked("UNKNOWN_EXERCISE_STEP:" + name);
            }
        }

        private static string BattleProofDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ServerIntegrationProof"));
        private static bool InBattleScene(string name) => SceneManager.GetActiveScene().name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0;
        private static CharacterComponent[] BattleActors() => UnityEngine.Object.FindObjectsByType<CharacterComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(actor => actor.gameObject.scene == SceneManager.GetActiveScene() && actor.stat != null && actor.gameObject.activeInHierarchy).ToArray();
        private static JArray BattleActorEvidence() => new JArray(BattleActors().Select(actor => new JObject
        {
            ["name"] = actor.name, ["key"] = actor.info?.key, ["faction"] = actor.factionType.ToString(),
            ["health"] = actor.stat.health.ToString(CultureInfo.InvariantCulture), ["health_max"] = actor.stat.healthMax.ToString(CultureInfo.InvariantCulture),
            ["attack_power"] = actor.stat.attackPower, ["defence"] = actor.stat.defenceValue, ["alive"] = actor.isLive, ["state"] = actor.stateType.ToString()
        }));

        private static void BattleProgress(string step, JObject progress)
        {
            progress["step"] = step; progress["uid"] = GameServer.Uid; progress["updated_at"] = DateTime.UtcNow.ToString("O");
            progress["scene"] = SceneManager.GetActiveScene().name;
            WriteJson(Path.Combine(BattleProofDirectory, "battle-progress.json"), progress);
        }

        private static async UniTask<JObject> EnterTournamentProofAsync(CancellationToken token)
        {
            if (!InBattleScene("Lobby")) throw new Blocked("LOBBY_SCENE_REQUIRED_FOR_TOURNAMENT_ENTRY");
            var worker = TournamentWorker.instance;
            if (worker.isRunning) throw new Blocked("TOURNAMENT_ALREADY_RUNNING");
            await worker.InitailizeAsync();
            var snapshot = (await GameServer.Tournament.SnapshotAsync(new TournamentEmptyReq(), GameServer.Options())).Data;
            if (snapshot.User == null || snapshot.SeasonStatus != TournamentAvailabilityStatus.Open) throw new Blocked("TOURNAMENT_NOT_OPEN", Details(snapshot));
            if (snapshot.User.ActiveBattleId != null) throw new Blocked("EXISTING_TOURNAMENT_BATTLE_REQUIRES_RESUME", Details(snapshot));
            if (snapshot.User.RemainingCount <= 0) throw new Blocked("TOURNAMENT_NO_ENTRIES_LEFT", Details(snapshot));
            var candidate = snapshot.User.Candidates.FirstOrDefault(row => row.Uid == 30) ?? snapshot.User.Candidates.FirstOrDefault();
            if (candidate == null) throw new Blocked("NO_TOURNAMENT_CANDIDATES_REFRESH_FIRST", Details(snapshot));
            var beforeAsset = (await GameServer.Currency.InfoAsync(new EmptyRes(), GameServer.Options())).Data;
            await worker.EnterBattleAsync(checked((int)candidate.Uid));
            var entry = worker.ServerEntry;
            if (!worker.isRunning || entry == null) throw new Blocked("TOURNAMENT_ENTRY_REJECTED:" + GameServer.LastError);
            var actorsExpected = entry.Attack.Heroes.Count + entry.Defense.Heroes.Count;
            await WaitForAsync(() => InBattleScene("Tournament") && BattleActors().Count(actor => actor.info != null) >= actorsExpected, token);
            var checks = new List<JObject>
            {
                Check("entry_target", candidate.Uid, entry.TargetUid),
                Check("attack_snapshot_table", GameServer.TableVersion, entry.Attack.TableVersion),
                Check("attack_view_heroes", entry.Attack.Heroes.Select(row => row.CharacterKey).OrderBy(key => key).ToArray(), worker.GetBatchData(true).heroes.Select(row => row.key).OrderBy(key => key).ToArray()),
                Check("defense_view_heroes", entry.Defense.Heroes.Select(row => row.CharacterKey).OrderBy(key => key).ToArray(), worker.enterUserData.batchData.heroes.Select(row => row.key).OrderBy(key => key).ToArray())
            };
            foreach (var side in new[] { (snapshot: entry.Attack, faction: FactionType.Alliance), (snapshot: entry.Defense, faction: FactionType.Enemy) })
                foreach (var expected in side.snapshot.Heroes)
                {
                    var actor = BattleActors().SingleOrDefault(row => row.factionType == side.faction && row.info?.key == expected.CharacterKey);
                    checks.Add(Check(side.faction + "_" + expected.CharacterKey + "_max_hp", expected.CombatStats.HealthMax, actor?.stat.healthMax));
                    checks.Add(Check(side.faction + "_" + expected.CharacterKey + "_attack", true, actor != null && Math.Abs(actor.stat.attackPower - expected.CombatStats.AttackPower) <= Math.Max(.0001, Math.Abs(expected.CombatStats.AttackPower) * .00001)));
                }
            var context = new JObject { ["uid"] = GameServer.Uid, ["battle_id"] = entry.BattleId, ["before_score"] = snapshot.User.Score,
                ["before_asset"] = Json(beforeAsset), ["entry"] = Json(entry), ["actors"] = BattleActorEvidence(), ["entered_at"] = DateTime.UtcNow.ToString("O") };
            WriteJson(Path.Combine(BattleProofDirectory, "tournament-entry.json"), context);
            return Details(context, checks.ToArray());
        }

        private static async UniTask<JObject> SettleTournamentProofAsync(CancellationToken token)
        {
            var worker = TournamentWorker.instance;
            var entry = worker.ServerEntry;
            if (!InBattleScene("Tournament") || !worker.isRunning || entry == null) throw new Blocked("ACTUAL_TOURNAMENT_SCENE_AND_ENTRY_REQUIRED");
            var timer = Stopwatch.StartNew();
            var limit = Math.Min(150, Math.Max(65, entry.BattleDurationSeconds + 45));
            while (worker.ServerResult == null && timer.Elapsed.TotalSeconds < limit)
            {
                token.ThrowIfCancellationRequested(); EnsureSession();
                if (!InBattleScene("Tournament") || worker.ServerEntry?.BattleId != entry.BattleId) throw new Blocked("TOURNAMENT_SCENE_OR_BATTLE_CHANGED");
                BattleProgress("tournament_settle", new JObject { ["status"] = "waiting_for_actual_battle_result", ["battle_id"] = entry.BattleId,
                    ["elapsed_seconds"] = timer.Elapsed.TotalSeconds, ["limit_seconds"] = limit, ["local_status"] = worker.statusType.ToString(), ["actors"] = BattleActorEvidence() });
                // The actual Scene_Tournament combat/timeout flow owns Finished() and the finish API request.
                await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: token);
            }
            if (worker.ServerResult == null) throw new Blocked("ACTUAL_TOURNAMENT_RESULT_NOT_READY_WITHIN_BOUND", Details(new { battle_id = entry.BattleId, elapsed_seconds = timer.Elapsed.TotalSeconds, actors = BattleActorEvidence() }));
            var result = worker.ServerResult;
            var history = (await GameServer.Tournament.HistoryAsync(new TournamentHistoryReq { Limit = 50 }, GameServer.Options())).Data;
            var saved = history.Battles.SingleOrDefault(row => row.BattleId == entry.BattleId && row.AttackerUid == GameServer.Uid);
            var snapshot = (await GameServer.Tournament.SnapshotAsync(new TournamentEmptyReq(), GameServer.Options())).Data;
            var asset = (await GameServer.Currency.InfoAsync(new EmptyRes(), GameServer.Options())).Data;
            var observedWin = TournamentHeroInfoManager.instance.IsWin();
            var expectedPoint = observedWin ? entry.PointWin : entry.PointLose;
            var evidence = new JObject { ["entry"] = Json(entry), ["actual_result"] = Json(result), ["history"] = Json(saved),
                ["server_snapshot"] = Json(snapshot), ["server_asset"] = Json(asset), ["actors"] = BattleActorEvidence(), ["elapsed_seconds"] = timer.Elapsed.TotalSeconds,
                ["result_source"] = "Existing Scene_Tournament combat/timeout and TournamentWorker finish flow; proof did not report a result." };
            var checks = new List<JObject>
            {
                Check("result_battle_id", entry.BattleId, result.BattleId), Check("actual_scene_outcome", observedWin, result.Result == TournamentBattleResult.Win),
                Check("history_completed", TournamentBattleStatus.Completed, saved?.Status), Check("history_result", result.Result, saved?.Result),
                Check("history_score_delta", result.AttackerScoreDelta, saved?.ScoreDelta), Check("point_delta_matches_entry_rule", expectedPoint, result.PointDelta),
                Check("view_score", snapshot.User?.Score, worker.rankData?.point), Check("view_tournament_point", asset.TournamentPoint, ServerState.Asset?.TournamentPoint),
                Check("active_battle_cleared", null, snapshot.User?.ActiveBattleId)
            };
            var contextPath = Path.Combine(BattleProofDirectory, "tournament-entry.json");
            if (File.Exists(contextPath))
            {
                var context = JObject.Parse(File.ReadAllText(contextPath));
                if ((string)context["battle_id"] == entry.BattleId && (long?)context["uid"] == GameServer.Uid)
                {
                    checks.Add(Check("persisted_score_delta", result.AttackerScoreDelta, snapshot.User.Score - (long)context["before_score"]));
                    checks.Add(Check("persisted_point_delta", result.PointDelta, asset.TournamentPoint - (long)context["before_asset"]["tournament_point"]));
                }
            }
            BattleProgress("tournament_settle", new JObject { ["status"] = "actual_battle_settled", ["battle_id"] = entry.BattleId, ["result"] = result.Result.ToString(), ["elapsed_seconds"] = timer.Elapsed.TotalSeconds });
            return Details(evidence, checks.ToArray());
        }

        private static async UniTask<JObject> EnterRaidProofAsync(CancellationToken token)
        {
            if (!InBattleScene("Lobby")) throw new Blocked("LOBBY_SCENE_REQUIRED_FOR_RAID_ENTRY");
            var raid = DataManager.bossRaid;
            if (BossRaidWorker.instance.isRunning) throw new Blocked("RAID_ALREADY_RUNNING");
            await raid.RefreshServerAsync();
            var round = raid.ServerRound;
            if (round == null || (round.Phase != "normal" && round.Phase != "jin") || (!round.CanJoin && raid.ServerRaid?.Participant == null)) throw new Blocked("RAID_ENTRY_NOT_CURRENTLY_AVAILABLE", Details(raid.ServerRaid));
            if (!Enum.TryParse(round.BossKey, out BossRaidWorker.BossRaidType type)) throw new Blocked("RAID_BOSS_VIEW_UNSUPPORTED:" + round.BossKey);
            await BossRaidWorker.instance.InitializeAsync(type);
            if (!raid.ServerJoined) throw new Blocked("RAID_JOIN_REJECTED:" + GameServer.LastError);
            await WaitForAsync(() => InBattleScene("BossRaid") && UnityEngine.Object.FindObjectsByType<Character_Enemy_RaidBoss>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Any(actor => actor.gameObject.scene == SceneManager.GetActiveScene() && actor.stat != null), token);
            return await RaidSnapshotProofAsync();
        }

        private static async UniTask<JObject> RaidSnapshotProofAsync()
        {
            var raid = DataManager.bossRaid;
            if (!InBattleScene("BossRaid") || !raid.ServerJoined || !BossRaidWorker.instance.isRunning) throw new Blocked("ACTUAL_JOINED_RAID_SCENE_REQUIRED");
            await raid.RefreshServerAsync();
            await raid.RefreshServerRankAsync(raid.ServerBattleId);
            var round = raid.ServerRound;
            var actors = UnityEngine.Object.FindObjectsByType<Character_Enemy_RaidBoss>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(actor => actor.gameObject.scene == SceneManager.GetActiveScene() && actor.stat != null).ToArray();
            var expectedStatus = round.Phase switch { "normal" => Data_BossRaid.BossRaidStatusType.FirstPhase, "transition" => Data_BossRaid.BossRaidStatusType.Finish_FirstPhase,
                "jin" => Data_BossRaid.BossRaidStatusType.SecondPhase, "finished" => Data_BossRaid.BossRaidStatusType.Finished, _ => Data_BossRaid.BossRaidStatusType.Wait };
            var checks = new List<JObject> { Check("joined_raid_id", raid.ServerBattleId, round.RaidId), Check("view_phase", expectedStatus, raid.raidStatus), Check("active_boss_actor_count", 1, actors.Length) };
            if (actors.Length == 1)
            {
                checks.Add(Check("view_boss_hp", round.Hp, actors[0].stat.health.ToString(CultureInfo.InvariantCulture)));
                checks.Add(Check("view_boss_max_hp", round.MaxHp, actors[0].stat.healthMax.ToString(CultureInfo.InvariantCulture)));
                checks.Add(Check("view_boss_attack", true, Math.Abs(actors[0].stat.attackPower - round.AttackPower) <= Math.Max(.0001, Math.Abs(round.AttackPower) * .00001)));
            }
            var labels = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(text => text.gameObject.scene == SceneManager.GetActiveScene() && text.name.IndexOf("timer", StringComparison.OrdinalIgnoreCase) >= 0).Select(text => Sanitize(text.text)).ToArray();
            var generation = raid.ServerSocketGeneration;
            return Details(new { round, participant = raid.ServerRanking?.Me, local_remaining_seconds = raid.ServerRemainingSeconds,
                socket_connected = raid.ServerSocketConnected, socket_generation = generation, restored_generation = raid.ServerSocketRestoredGeneration,
                pattern_epoch = round.PatternStartedAt, pattern_index = round.PatternIndex, hud_timer_text = labels, actors = BattleActorEvidence() }, checks.ToArray());
        }

        private static async UniTask<JObject> RaidReconnectProofAsync(CancellationToken token)
        {
            var raid = DataManager.bossRaid;
            if (!InBattleScene("BossRaid") || !raid.ServerJoined || !raid.ServerSocketConnected) throw new Blocked("CONNECTED_JOINED_RAID_SCENE_REQUIRED");
            if (raid.ServerRound?.Phase != "normal" && raid.ServerRound?.Phase != "jin") throw new Blocked("RAID_RECONNECT_REQUIRES_ACTIVE_COMBAT_PHASE");
            if (raid.ServerRemainingSeconds < 15) throw new Blocked("RAID_RECONNECT_TOO_CLOSE_TO_ROUND_END");
            await raid.RefreshServerRankAsync(raid.ServerBattleId);
            var beforeDamage = raid.ServerRanking?.Me?.Damage ?? "0";
            var battleId = raid.ServerBattleId;
            var generation = raid.ServerSocketGeneration;
            await raid.RefreshServerConnectionAsync();
            await WaitForAsync(() => raid.ServerSocketConnected && raid.ServerSocketGeneration > generation && raid.ServerSocketRestoredGeneration == raid.ServerSocketGeneration, token);
            await raid.RefreshServerRankAsync(battleId);
            var afterDamage = raid.ServerRanking?.Me?.Damage ?? "0";
            return Details(new { raid_id = battleId, previous_generation = generation, generation = raid.ServerSocketGeneration,
                restored_generation = raid.ServerSocketRestoredGeneration, damage_before = beforeDamage, damage_after = afterDamage,
                connection_scope = "Only the current Unity raid socket closed normally; existing recovery loop/request retry restored it." },
                Check("same_uid", 29L, GameServer.Uid), Check("same_raid", battleId, raid.ServerBattleId),
                Check("authenticated_new_generation", true, raid.ServerSocketGeneration > generation),
                Check("socket_state_restored", raid.ServerSocketGeneration, raid.ServerSocketRestoredGeneration),
                Check("damage_preserved", true, System.Numerics.BigInteger.Parse(afterDamage) >= System.Numerics.BigInteger.Parse(beforeDamage)));
        }

        private static async UniTask<JObject> RaidCombatInputProofAsync(CancellationToken token)
        {
            var raid = DataManager.bossRaid;
            if (!InBattleScene("BossRaid") || !raid.ServerJoined) throw new Blocked("ACTUAL_JOINED_RAID_SCENE_REQUIRED");
            var id = raid.ServerBattleId;
            await raid.RefreshServerRankAsync(id);
            var before = raid.ServerRanking?.Me?.Damage ?? "0";
            var timer = Stopwatch.StartNew();
            var successfulInputs = 0;
            var observations = new JArray();
            while (timer.Elapsed.TotalSeconds < 25 && InBattleScene("BossRaid") && raid.ServerBattleId == id)
            {
                token.ThrowIfCancellationRequested();
                var phase = raid.ServerRound?.Phase;
                if (phase == "finished") break;
                var hero = TeamManager.instance?.mainHero;
                if ((phase == "normal" || phase == "jin") && hero != null && hero.isLive && ControllerManager.instance.isSwitch)
                {
                    var input = await ServerInputProof.ButtonTouchAsync("attack", token);
                    if ((bool?)input["checks"]?[0]?["passed"] == true) successfulInputs++;
                    observations.Add(new JObject { ["seconds"] = timer.Elapsed.TotalSeconds, ["phase"] = phase,
                        ["attack_count"] = hero.attack.controlAttackCount, ["boss_hp"] = raid.ServerRound?.Hp });
                }
                await UniTask.Delay(400, ignoreTimeScale: true, cancellationToken: token);
            }
            await UniTask.Delay(1200, ignoreTimeScale: true, cancellationToken: token);
            await raid.RefreshServerRankAsync(id);
            var after = raid.ServerRanking?.Me?.Damage ?? "0";
            return Details(new { raid_id = id, damage_before = before, damage_after = after, successful_inputs = successfulInputs,
                elapsed_seconds = timer.Elapsed.TotalSeconds, observations,
                input_source = "Real Unity EventSystem attack button gestures; game animation and collision submit damage. No position, HP, damage value or outcome is injected." },
                Check("actual_attack_executed", true, successfulInputs > 0),
                Check("measured_damage_persisted", true, System.Numerics.BigInteger.Parse(after) > System.Numerics.BigInteger.Parse(before)));
        }

        private static async UniTask WaitForAsync(Func<bool> condition, CancellationToken token)
        {
            for (var attempt = 0; attempt < 150; attempt++)
            {
                if (condition()) return;
                token.ThrowIfCancellationRequested();
                if (!Application.isPlaying) throw new OperationCanceledException();
                await UniTask.Delay(100, ignoreTimeScale: true, cancellationToken: token);
            }
            throw new TimeoutException("SCENE_TRANSITION_TIMEOUT");
        }

        private static JObject CaptureState() => new JObject
        {
            ["playing"] = Application.isPlaying, ["uid"] = GameServer.IsLoggedIn ? GameServer.Uid : 0,
            ["region_selected"] = GameServer.Login?.RegionSelected ?? false, ["table_hash"] = GameServer.TableVersion,
            ["asset"] = Json(ServerState.Asset),
            ["characters"] = Json(ServerState.Characters?.Characters.Select(hero => new { id = hero.CharacterId, key = hero.CharacterKey, growth = hero.GrowthStage, grade = hero.CharacterGrade, revision = hero.Revision })),
            ["castle_revision"] = DataManager.castle.ServerCastle?.Revision,
            ["castle_buildings"] = Json(DataManager.castle.ServerCastle?.Buildings.Select(row => new { id = row.BuildingId, key = row.BuildingKey, level = row.Level, assigned = row.AssignedCharacterIds, production = row.Production?.ProductionAmount })),
            ["scene_name"] = SceneManager.GetActiveScene().name,
            ["input_diagnostics"] = ServerInputProof.Capture()
        };

        private static JObject CaptureScene()
        {
            var scenes = new JArray();
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                scenes.Add(new JObject { ["name"] = scene.name, ["loaded"] = scene.isLoaded,
                    ["roots"] = Json(scene.GetRootGameObjects().Select(root => new { name = root.name, active = root.activeInHierarchy, children = root.transform.childCount })) });
            }
            return new JObject
            {
                ["loaded_scenes"] = scenes,
                ["active_event_systems"] = Json(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Select(events => new { name = events.name, scene = events.gameObject.scene.name }).ToArray()),
                ["active_global_lights"] = Json(UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Where(light => light.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global)
                    .Select(light => new { name = light.name, scene = light.gameObject.scene.name }).ToArray()),
                ["input_diagnostics"] = ServerInputProof.Capture(),
                ["visible_text"] = Json(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Where(text => text.isActiveAndEnabled).Take(80).Select(text => new { name = text.name, text = Sanitize(text.text) }))
            };
        }

        private static string Sanitize(string message)
        {
            if (message == null) return null;
            var secret = GameServer.Settings.ApiSecretKey;
            if (!string.IsNullOrEmpty(secret)) message = message.Replace(secret, "[REDACTED]");
            if (!string.IsNullOrEmpty(GameServer.SessionKey)) message = message.Replace(GameServer.SessionKey, "[REDACTED]");
            return message;
        }

        public static void WriteJson(string path, JObject value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, value.ToString(Formatting.Indented));
            // Windows can briefly retain the previous report while a reader closes it.
            // Preserve atomic publication and retry only this bounded sharing window.
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                    return;
                }
                catch (IOException) when (attempt < 3 && File.Exists(temporary) && File.Exists(path))
                {
                    Thread.Sleep(20 * (attempt + 1));
                }
            }
        }
    }
}
#endif
