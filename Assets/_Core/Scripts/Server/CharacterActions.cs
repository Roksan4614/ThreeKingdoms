using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using ThreeKingdoms.Shared.Rest;
using ThreeKingdoms.Shared.Types;

namespace ThreeKingdoms.Client.Server
{
    // Character mutations share a queue and retain request IDs until their outcome is known.
    public static class CharacterActions
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private static readonly Dictionary<string, RestRequestOptions> Pending = new();
        private static async UniTask<T> ExecuteAsync<T>(string operation, object request, Func<RestRequestOptions, UniTask<T>> action)
        {
            await Gate.WaitAsync();
            try { return await ExecuteLockedAsync(operation, request, action); }
            finally { Gate.Release(); }
        }
        private static async UniTask<T> ExecuteLockedAsync<T>(string operation, object request, Func<RestRequestOptions, UniTask<T>> action)
        {
            var key = GameServer.Uid + ":" + GameServer.TableVersion + ":" + operation + ":" + JsonConvert.SerializeObject(request);
            try
            {
                if (!Pending.TryGetValue(key, out var options)) Pending[key] = options = GameServer.Options();
                var result = await action(options);
                Pending.Remove(key);
                return result;
            }
            catch (GameServerException error)
            {
                // A malformed response or a server error may follow a committed
                // transaction. Only a definite rejection can start a new attempt.
                if (error.HttpStatus >= 200 && error.HttpStatus < 500
                    && error.Code != "SERVER_RESPONSE_INVALID" && error.Code != "SERVER_HTTP_ERROR") Pending.Remove(key);
                throw;
            }
        }
        public static async UniTask RefreshAsync()
        {
            var response = await GameServer.Character.LobbyAsync(new EmptyRes(), GameServer.Options());
            await ServerState.ApplyCharactersAsync(response.Data ?? throw new InvalidOperationException("Character snapshot is empty."));
        }
        public static UniTask<CharacterGrowRes> GrowAsync(string heroKey)
        {
            var request = new CharacterGrowReq { CharacterId = ServerState.CharacterId(heroKey) };
            return ExecuteAsync("grow", request, async options =>
            {
                var result = (await GameServer.Character.GrowAsync(request, options)).Data
                    ?? throw new InvalidOperationException("Character growth response is empty.");
                ServerState.ApplyAsset(result.Asset);
                await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
                return result;
            });
        }
        public static UniTask<CharacterAscendRes> AscendAsync(string heroKey, GradeType grade)
        {
            var request = new CharacterAscendReq
            { CharacterId = ServerState.CharacterId(heroKey), TargetGrade = (ThreeKingdoms.Shared.Enums.CharacterGrade)(int)grade };
            return ExecuteAsync("ascend", request, async options =>
            {
                var result = (await GameServer.Character.AscendAsync(request, options)).Data
                    ?? throw new InvalidOperationException("Character ascension response is empty.");
                ServerState.ApplyItems(result.ItemUpdates);
                await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
                return result;
            });
        }
        public static UniTask<HeroInfoData> RerollTraitsAsync(string heroKey)
        {
            var request = new CharacterRerollTraitsReq { CharacterId = ServerState.CharacterId(heroKey) };
            return ExecuteAsync("reroll-traits", request, async options =>
            {
                var result = (await GameServer.Character.RerollTraitsAsync(request, options)).Data
                    ?? throw new InvalidOperationException("Character trait response is empty.");
                ServerState.ApplyAsset(result.Asset);
                await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
                return ServerState.ToHero(ServerState.Character(heroKey));
            });
        }
        public static UniTask<HeroInfoData> SetTraitLockAsync(string heroKey, int slot, bool locked)
        {
            var request = new CharacterSetTraitLockReq { CharacterId = ServerState.CharacterId(heroKey), SlotIndex = slot, IsLocked = locked };
            return ExecuteAsync("set-trait-lock", request, async options =>
            {
                var result = (await GameServer.Character.SetTraitLockAsync(request, options)).Data
                    ?? throw new InvalidOperationException("Character trait lock response is empty.");
                await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
                return ServerState.ToHero(ServerState.Character(heroKey));
            });
        }
        public static UniTask<HeroInfoData> EnhanceRelicAsync(string heroKey)
        {
            var hero = ServerState.Character(heroKey) ?? throw new InvalidOperationException("Owned character is missing.");
            if (hero.Relic.NextEnhance == null) throw new InvalidOperationException("현재 등급에서 유물을 더 강화할 수 없습니다.");
            var request = new RelicEnhanceReq { CharacterId = hero.CharacterId, TargetStage = hero.Relic.EnhanceStage + 1 };
            return ExecuteAsync("relic-enhance", request, async options =>
            {
                var result = (await GameServer.Character.RelicEnhanceAsync(request, options)).Data
                    ?? throw new InvalidOperationException("Relic enhancement response is empty.");
                ServerState.ApplyAsset(result.Asset);
                await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
                return ServerState.ToHero(ServerState.Character(heroKey));
            });
        }
        public static async UniTask<bool> SetTreasureAsync(string treasureKey, bool equipped)
        {
            await Gate.WaitAsync();
            try
            {
                // Read the selection after earlier queued changes have applied.
                // Otherwise fast clicks on A and B would send [A], then stale [B].
                var owned = ServerState.Characters?.Treasures ?? throw new InvalidOperationException("Treasure snapshot is missing.");
                var target = owned.FirstOrDefault(x => x.TreasureKey == treasureKey)
                    ?? throw new InvalidOperationException("보유하지 않은 보물입니다.");
                var ids = owned.Where(x => x.EquippedSlot.HasValue && x.TreasureId != target.TreasureId)
                    .OrderBy(x => x.EquippedSlot).Select(x => x.TreasureId).ToList();
                if (equipped) ids.Add(target.TreasureId);
                if (ids.Count > 3) throw new InvalidOperationException("보물은 최대 3개까지 장착할 수 있습니다.");
                var request = new TreasureSetEquippedReq { EquippedTreasureIds = ids };
                return await ExecuteLockedAsync("treasure-set-equipped", request, async options =>
                {
                    var result = (await GameServer.Character.TreasureSetEquippedAsync(request, options)).Data
                        ?? throw new InvalidOperationException("Treasure equip response is empty.");
                    await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
                    return true;
                });
            }
            finally { Gate.Release(); }
        }
        public static UniTask<CharacterSetPositionRes> SetPositionAsync(long positionId, long? characterId)
        {
            var request = new CharacterSetPositionReq { PositionId = positionId, CharacterId = characterId };
            return ExecuteAsync("set-position", request, async options =>
            {
                var result = (await GameServer.Character.SetPositionAsync(request, options)).Data
                    ?? throw new InvalidOperationException("Character position response is empty.");
                await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
                return result;
            });
        }
        public static long RerollCost(string heroKey)
        {
            var locked = ServerState.Character(heroKey)?.Traits.Count(x => x.IsLocked) ?? 0;
            var key = "CHARACTER_TRAITS_REROLL_COST_LOCK" + locked;
            var row = GameServer.TableRows("s_parameter").FirstOrDefault(x => (string)x["key"] == key);
            return row == null ? 0 : long.Parse((string)row["value"], CultureInfo.InvariantCulture);
        }
        public static long AscensionCost(string heroKey, GradeType target)
        {
            var current = ServerState.Character(heroKey) ?? throw new InvalidOperationException("Owned character is missing.");
            return GameServer.TableRows("s_ascension").Where(x => (int)x["character_grade"] > (int)current.CharacterGrade
                && (int)x["character_grade"] <= (int)target).Sum(x => (long)x["material_count"]);
        }
        public static GradeType GradeForTraitSlot(int slot)
        {
            var row = GameServer.TableRows("s_ascension").OrderBy(x => (int)x["character_grade"])
                .FirstOrDefault(x => (int)x["traits_slot"] > slot);
            return row == null ? GradeType.MAX : (GradeType)(int)row["character_grade"];
        }
    }
}
