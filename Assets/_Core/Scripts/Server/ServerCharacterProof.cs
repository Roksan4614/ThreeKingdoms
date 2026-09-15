#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThreeKingdoms.Shared.Enums;
using ThreeKingdoms.Shared.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThreeKingdoms.Client.Server
{
    // Explicit character proof steps. No fixture grant, unlock bypass, login, or automatic execution.
    public static class ServerCharacterProof
    {
        private static readonly HashSet<string> Steps = new HashSet<string>
        {
            "character_ascend_next", "character_traits_reroll", "character_trait_lock", "character_trait_unlock",
            "character_relic_enhance", "character_treasure_equip", "character_position_assign"
        };
        public static bool Handles(string step) => Steps.Contains(step);

        // Caller should propagate status=blocked as its own precondition result, rather than counting it as a passed mutation.
        public static async UniTask<JObject> RunAsync(string step, CancellationToken token = default)
        {
            var proof = new JObject { ["step"] = step, ["status"] = "running", ["scope"] = "Existing CharacterActions and owned/unlocked state only; no synthetic progression." };
            try
            {
                if (!Handles(step)) return Block(proof, "UNKNOWN_CHARACTER_PROOF_STEP");
                if (!Application.isPlaying || !GameServer.Enabled || !GameServer.IsLoggedIn) return Block(proof, "CURRENT_PLAYMODE_LOGIN_REQUIRED");
                if (GameServer.Uid != 29 || GameServer.Settings.TestUid != 29) return Block(proof, "DEDICATED_UID29_REQUIRED");
                var api = new Uri(GameServer.Settings.ApiBaseUrl);
                if (!api.IsLoopback || api.Port != 11080) return Block(proof, "EXISTING_LOCAL_API_11080_REQUIRED");
                if (GameServer.Login?.RegionSelected != true) return Block(proof, "REGION_NOT_SELECTED");
                if (SceneManager.GetActiveScene().name.IndexOf("Lobby", StringComparison.OrdinalIgnoreCase) < 0) return Block(proof, "LOBBY_REQUIRED_FOR_CHARACTER_CHANGES");
                proof["uid"] = GameServer.Uid;
                token.ThrowIfCancellationRequested();
                await CharacterActions.RefreshAsync();
                var before = ServerState.Characters.Characters.FirstOrDefault();
                if (before == null) return Block(proof, "NO_OWNED_CHARACTER");
                var beforeAsset = (await GameServer.Currency.InfoAsync(new EmptyRes(), GameServer.Options(), token)).Data;
                proof["before"] = JToken.FromObject(before);
                proof["before_asset"] = JToken.FromObject(beforeAsset);
                var checks = new JArray();
                proof["checks"] = checks;
                long? positionId = null;
                string positionKey = null;
                string treasureKey = null;
                int? traitSlot = null;
                bool? targetLock = null;
                switch (step)
                {
                    case "character_ascend_next":
                        var target = (int)before.CharacterGrade + 1;
                        if (!Enum.IsDefined(typeof(CharacterGrade), target)) return Block(proof, "CHARACTER_ALREADY_AT_MAX_GRADE");
                        var soul = GameServer.TableRows("s_item").Single(row => (string)row["character_key"] == before.CharacterKey && ((string)row["key"]).StartsWith("dedicated_soul_stone_"));
                        var soulId = (long)soul["idx"];
                        var cost = CharacterActions.AscensionCost(before.CharacterKey, (GradeType)target);
                        var owned = ServerState.Characters.Items.SingleOrDefault(item => item.ItemId == soulId)?.Amount ?? 0;
                        proof["required"] = new JObject { ["target_grade"] = target, ["item_id"] = soulId, ["amount"] = cost, ["owned"] = owned };
                        if (owned < cost) return Block(proof, "NOT_ENOUGH_OWNED_ASCENSION_MATERIAL");
                        var ascended = await CharacterActions.AscendAsync(before.CharacterKey, (GradeType)target);
                        proof["event_id"] = ascended.EventId;
                        checks.Add(Check("grade_advanced_one", target, (int)ServerState.Character(before.CharacterKey).CharacterGrade));
                        break;
                    case "character_traits_reroll":
                        if (before.Traits.Count == 0) return Block(proof, "NO_UNLOCKED_TRAIT_SLOTS");
                        if (before.Traits.All(trait => trait.IsLocked)) return Block(proof, "ALL_TRAITS_LOCKED");
                        var riceCost = CharacterActions.RerollCost(before.CharacterKey);
                        proof["required"] = new JObject { ["rice"] = riceCost, ["owned_rice"] = beforeAsset.Rice };
                        if (beforeAsset.Rice < riceCost) return Block(proof, "NOT_ENOUGH_RICE_FOR_REROLL");
                        await CharacterActions.RerollTraitsAsync(before.CharacterKey);
                        foreach (var locked in before.Traits.Where(trait => trait.IsLocked))
                            checks.Add(Check("locked_slot_" + locked.SlotIndex + "_preserved", locked, ServerState.Character(before.CharacterKey).Traits.Single(trait => trait.SlotIndex == locked.SlotIndex)));
                        checks.Add(Check("trait_slot_count", before.Traits.Count, ServerState.Character(before.CharacterKey).Traits.Count));
                        proof["random_outcome_note"] = "An unlocked slot may randomly roll the same values; the proof does not demand a different result.";
                        break;
                    case "character_trait_lock":
                    case "character_trait_unlock":
                        targetLock = step == "character_trait_lock";
                        var trait = before.Traits.FirstOrDefault(item => item.IsLocked != targetLock.Value);
                        if (trait == null) return Block(proof, before.Traits.Count == 0 ? "NO_UNLOCKED_TRAIT_SLOTS" : "ALL_TRAITS_ALREADY_IN_REQUESTED_LOCK_STATE");
                        traitSlot = trait.SlotIndex;
                        await CharacterActions.SetTraitLockAsync(before.CharacterKey, trait.SlotIndex, targetLock.Value);
                        var afterTrait = ServerState.Character(before.CharacterKey).Traits.Single(item => item.SlotIndex == trait.SlotIndex);
                        checks.Add(Check("trait_lock_state", targetLock.Value, afterTrait.IsLocked));
                        checks.Add(Check("trait_key_unchanged", trait.TraitKey, afterTrait.TraitKey));
                        checks.Add(Check("trait_values_unchanged", trait.Values, afterTrait.Values));
                        break;
                    case "character_relic_enhance":
                        if (before.Relic.NextEnhance == null) return Block(proof, "RELIC_AT_CURRENT_GRADE_CAP");
                        proof["required"] = new JObject { ["time_stone"] = before.Relic.NextEnhance.CostTimeStone, ["owned_time_stone"] = beforeAsset.TimeStone };
                        if (beforeAsset.TimeStone < before.Relic.NextEnhance.CostTimeStone) return Block(proof, "NOT_ENOUGH_TIME_STONE");
                        await CharacterActions.EnhanceRelicAsync(before.CharacterKey);
                        checks.Add(Check("relic_advanced_one", before.Relic.EnhanceStage + 1, ServerState.Character(before.CharacterKey).Relic.EnhanceStage));
                        break;
                    case "character_treasure_equip":
                        var treasures = ServerState.Characters.Treasures;
                        if (treasures.Count == 0) return Block(proof, "NO_OWNED_TREASURE");
                        if (treasures.Count(item => item.EquippedSlot.HasValue) >= 3) return Block(proof, "TREASURE_SLOTS_FULL");
                        var unequipped = treasures.FirstOrDefault(item => !item.EquippedSlot.HasValue);
                        if (unequipped == null) return Block(proof, "NO_UNEQUIPPED_OWNED_TREASURE");
                        treasureKey = unequipped.TreasureKey;
                        await CharacterActions.SetTreasureAsync(treasureKey, true);
                        checks.Add(Check("treasure_equipped", true, ServerState.Characters.Treasures.Single(item => item.TreasureKey == treasureKey).EquippedSlot.HasValue));
                        break;
                    case "character_position_assign":
                        var positions = (await GameServer.Character.PositionSyncAsync(new EmptyRes(), GameServer.Options(), token)).Data.Positions;
                        var available = positions.FirstOrDefault(position => position.Status == CharacterPositionStatus.Unlocked && position.CharacterId == null && position.EligibleCharacterIds.Contains(before.CharacterId));
                        proof["available_positions"] = JToken.FromObject(positions);
                        if (available == null) return Block(proof, "NO_EMPTY_UNLOCKED_ELIGIBLE_POSITION");
                        positionId = available.PositionId; positionKey = available.PositionKey;
                        var assigned = await CharacterActions.SetPositionAsync(positionId.Value, before.CharacterId);
                        proof["event_id"] = assigned.EventId;
                        await DataManager.heroPosition.RefreshServerAsync();
                        checks.Add(Check("position_assigned", before.CharacterId, assigned.Positions.Single(position => position.PositionId == positionId).CharacterId));
                        break;
                }
                token.ThrowIfCancellationRequested();
                var persisted = (await GameServer.Character.LobbyAsync(new EmptyRes(), GameServer.Options(), token)).Data;
                var server = persisted.Characters.Single(character => character.CharacterId == before.CharacterId);
                var cache = ServerState.Character(before.CharacterKey);
                var view = DataManager.userInfo.GetHeroInfoData(before.CharacterKey);
                proof["server_after"] = JToken.FromObject(server);
                proof["cache_after"] = JToken.FromObject(cache);
                checks.Add(Check("server_cache_character", server, cache));
                checks.Add(Check("view_grade", (int)server.CharacterGrade, (int)view.grade));
                checks.Add(Check("view_growth", server.GrowthStage, (long)view.enchantLevel));
                checks.Add(Check("view_relic", server.Relic.EnhanceStage, (long)view.relicLevel));
                foreach (var item in persisted.Items)
                    checks.Add(Check("inventory_item_" + item.ItemId, item.Amount, InventoryWorker.instance.GetItemCount(ServerState.ToItem(item.ItemId, 1))));
                if (traitSlot.HasValue)
                    checks.Add(Check("view_trait_lock", targetLock.Value, view.traits.Single(trait => trait.index == traitSlot).isLock));
                if (positionId.HasValue)
                {
                    var type = CharacterPositionCatalog.FromServerKey(positionKey);
                    checks.Add(Check("view_position", type, view.positionType));
                    checks.Add(Check("position_cache", before.CharacterId, DataManager.heroPosition.ServerPosition(type)?.CharacterId));
                }
                if (treasureKey != null)
                {
                    checks.Add(Check("persisted_treasure_slot", persisted.Treasures.Single(item => item.TreasureKey == treasureKey).EquippedSlot,
                        ServerState.Characters.Treasures.Single(item => item.TreasureKey == treasureKey).EquippedSlot));
                }
                var asset = (await GameServer.Currency.InfoAsync(new EmptyRes(), GameServer.Options(), token)).Data;
                proof["asset_after"] = JToken.FromObject(asset);
                checks.Add(Check("server_cache_asset", asset, ServerState.Asset));
                proof["status"] = checks.All(check => (bool)check["passed"]) ? "passed" : "failed";
            }
            catch (GameServerException error)
            {
                proof["status"] = error.HttpStatus >= 200 && error.HttpStatus < 500 ? "blocked" : "failed";
                proof["reason"] = error.Code; proof["request_id"] = error.RequestId; proof["http_status"] = error.HttpStatus;
                if ((string)proof["status"] == "failed") AddFailureCheck(proof, error.Code);
            }
            catch (OperationCanceledException) { proof["status"] = "blocked"; proof["reason"] = "CHARACTER_PROOF_CANCELLED"; }
            catch (Exception error)
            {
                var reason = error.GetType().Name + ": " + error.Message;
                if (!string.IsNullOrEmpty(GameServer.Settings.ApiSecretKey)) reason = reason.Replace(GameServer.Settings.ApiSecretKey, "[REDACTED]");
                if (!string.IsNullOrEmpty(GameServer.SessionKey)) reason = reason.Replace(GameServer.SessionKey, "[REDACTED]");
                proof["status"] = "failed"; proof["reason"] = reason; AddFailureCheck(proof, reason);
            }
            return proof;
        }

        private static JObject Block(JObject proof, string reason) { proof["status"] = "blocked"; proof["reason"] = reason; return proof; }
        private static void AddFailureCheck(JObject proof, string reason)
        {
            if (!(proof["checks"] is JArray checks)) { checks = new JArray(); proof["checks"] = checks; }
            checks.Add(Check("character_action_completed_without_error", "success", reason));
        }
        private static JToken Json(object value) => value == null ? JValue.CreateNull() : JToken.FromObject(value);
        private static JObject Check(string name, object expected, object actual) => new JObject
        { ["name"] = name, ["expected"] = Json(expected), ["actual"] = Json(actual), ["passed"] = JToken.DeepEquals(Json(expected), Json(actual)) };
    }
}
#endif
