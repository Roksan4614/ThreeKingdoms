using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThreeKingdoms.Shared.Types;

namespace ThreeKingdoms.Client.Server
{
    public static class ServerState
    {
        private static readonly Dictionary<long, ItemStackDto> Items = new Dictionary<long, ItemStackDto>();
        private static readonly Dictionary<string, CharacterStateDto> Heroes = new Dictionary<string, CharacterStateDto>();
        public static AssetDto Asset { get; private set; }
        public static CharacterSnapshotDto Characters { get; private set; }
        private static long? _raidPoints;
        public static event Action Changed;
        public static void Reset() { Asset = null; Characters = null; _raidPoints = null; Items.Clear(); Heroes.Clear(); }
        public static void ApplyRaidPoints(string balance)
        {
            _raidPoints = long.Parse(balance, System.Globalization.CultureInfo.InvariantCulture);
            if (_raidPoints < 0) throw new InvalidOperationException("INVALID_RAID_POINT_BALANCE");
            UpdateInventory();
            Changed?.Invoke();
        }
        private static bool Older(string incoming, string current)
        {
            if (incoming == null || incoming.Any(c => c < '0' || c > '9')) throw new InvalidOperationException("INVALID_SERVER_REVISION");
            return current != null && (incoming.Length < current.Length || (incoming.Length == current.Length && string.CompareOrdinal(incoming, current) < 0));
        }
        public static CharacterStateDto Character(string key) => Heroes.TryGetValue(key, out var hero) ? hero : null;
        public static long CharacterId(string key) => Character(key)?.CharacterId ?? throw new InvalidOperationException("SERVER_CHARACTER_MISSING: " + key);
        public static void ApplyAsset(AssetDto asset)
        {
            if (asset == null || (Asset != null && Older(asset.Revision, Asset.Revision))) return;
            Asset = asset;
            DataManager.userInfo.SetAsset(checked(asset.FreeGold + asset.PaidGold), asset.Rice);
            UpdateInventory();
            Changed?.Invoke();
        }
        public static void ApplyItems(IReadOnlyList<ItemStackDto> items)
        {
            if (items == null) return;
            foreach (var item in items)
                if (!Items.TryGetValue(item.ItemId, out var previous) || !Older(item.Revision, previous.Revision)) Items[item.ItemId] = item;
            UpdateInventory();
            Changed?.Invoke();
        }
        public static void UpdateInventory()
        {
            var items = Items.Values.Select(item => ToItem(item.ItemId, item.Amount)).Where(item => item != null).ToList();
            if (Asset != null)
            {
                var timeStone = GameServer.TableRows("s_item").OfType<JObject>().Single(row => (string)row["key"] == "time_stone");
                items.Add(ToItem((long)timeStone["idx"], Asset.TimeStone));
                var point = GameServer.TableRows("s_item").OfType<JObject>().Single(row => (string)row["key"] == "tournament_point");
                items.Add(ToItem((long)point["idx"], Asset.TournamentPoint));
            }
            if (_raidPoints.HasValue)
            {
                var row = GameServer.TableRows("s_item").OfType<JObject>().Single(item => (string)item["key"] == "raid_point");
                items.Add(ToItem((long)row["idx"], _raidPoints.Value));
            }
            InventoryWorker.instance.ApplyServerInventory(items);
        }
        public static async UniTask ApplyCharactersAsync(CharacterSnapshotDto snapshot)
        {
            if (snapshot == null) return;
            if (snapshot.TableVersion != GameServer.TableVersion) throw new GameServerException("CHARACTER_TABLE_VERSION_MISMATCH", 0);
            var acceptsCollections = Heroes.Keys.All(key => snapshot.Characters.Any(hero => hero.CharacterKey == key))
                && snapshot.Characters.All(hero => !Heroes.TryGetValue(hero.CharacterKey, out var previous) || !Older(hero.CombatRevision, previous.CombatRevision));
            foreach (var hero in snapshot.Characters)
                if (!Heroes.TryGetValue(hero.CharacterKey, out var previous) || !Older(hero.CombatRevision, previous.CombatRevision)) Heroes[hero.CharacterKey] = hero;
            ApplyItems(snapshot.Items);
            Characters = new CharacterSnapshotDto
            {
                TableVersion = snapshot.TableVersion,
                Characters = Heroes.Values.ToList(),
                Items = Items.Values.ToList(),
                BootstrapCharacterId = acceptsCollections ? snapshot.BootstrapCharacterId : Characters?.BootstrapCharacterId,
                Treasures = acceptsCollections ? snapshot.Treasures : Characters?.Treasures ?? new List<TreasureStateDto>()
            };
            DataManager.userInfo.ApplyServerCharacters(Heroes.Values.Select(ToHero).ToList(), Characters.Treasures);
            if (AddressableManager.instance.curSceneName?.Contains("Lobby") == true)
                foreach (var hero in Heroes.Values) Signal.instance.UpdateHeroStat.Emit(hero.CharacterKey);
            Changed?.Invoke();
            await UniTask.CompletedTask;
        }
        public static HeroInfoData ToHero(CharacterStateDto hero)
        {
            var previous = DataManager.userInfo.myHero?.FirstOrDefault(item => item.key == hero.CharacterKey);
            var result = new HeroInfoData(hero.CharacterKey, (GradeType)(int)hero.CharacterGrade,
                _enchantLevel: checked((int)hero.GrowthStage), _relicLevel: checked((int)hero.Relic.EnhanceStage),
                _isBatch: previous?.isBatch ?? (Characters?.BootstrapCharacterId == hero.CharacterId),
                _isMain: previous?.isMain ?? (Characters?.BootstrapCharacterId == hero.CharacterId), _isMine: true,
                _statData: ToCombatStats(hero.CombatStats));
            result.serverCharacterId = hero.CharacterId;
            result.serverCombatPower = hero.CombatPower;
            result.traits = hero.Traits.Select(trait => new HeroTraitsData
            {
                index = trait.SlotIndex,
                type = Enum.Parse<TraitsType>(trait.TraitKey),
                indexValue = (int)trait.TraitValueGrade,
                isLock = trait.IsLocked,
                serverValues = trait.Values.OrderBy(entry => int.Parse(entry.Key)).Select(entry => entry.Value).ToArray()
            }).ToList();
            if (hero.PositionId.HasValue)
            {
                var row = GameServer.TableRows("s_position").OfType<JObject>().Single(item => (long)item["idx"] == hero.PositionId.Value);
                result.positionType = CharacterPositionCatalog.FromServerKey((string)row["key"]);
            }
            return result;
        }
        public static TableStatData ToCombatStats(CharacterCombatStatsDto stats)
        {
            var result = JsonConvert.DeserializeObject<TableStatData>(JsonConvert.SerializeObject(stats));
            result.health = result.healthMax;
            return result;
        }

        // 서버 concrete item ID를 기존 아이콘/연출 모델로 투영한다. 이 메서드는 지급하지 않는다.
        public static ItemData ToItem(long itemId, long count)
        {
            var row = GameServer.TableRows("s_item").OfType<JObject>().Single(item => (long)item["idx"] == itemId);
            var key = (string)row["key"];
            var value = "";
            ItemType type;
            if (key == "free_gold" || key == "paid_gold") type = ItemType.gold;
            else if (key.StartsWith("treasure_piece_") && !key.Contains("random_box")) { type = ItemType.treasure_piece; value = (string)row["treasure_key"]; }
            else if (key.StartsWith("treasure_") && !key.Contains("random_box")) { type = ItemType.treasure; value = (string)row["treasure_key"]; }
            else if (key.StartsWith("dedicated_soul_stone_") && !key.Contains("random_box")) { type = ItemType.dedicated_soul_stone; value = (string)row["character_key"]; }
            else if (key.StartsWith("class_soul_stone_") && !key.Contains("random_box")) { type = ItemType.class_soul_stone; value = (string)row["character_class_key"]; }
            else if (key.Contains("_pocket_"))
            {
                type = Enum.Parse<ItemType>("bundle_" + key.Substring(key.LastIndexOf('_') + 1));
                value = key.Substring(0, key.IndexOf("_pocket_", StringComparison.Ordinal));
            }
            else if (key.Contains("_random_box_"))
            {
                type = Enum.Parse<ItemType>(key.Substring(0, key.LastIndexOf('_')));
                value = key.Substring(key.LastIndexOf('_') + 1);
            }
            else if (!Enum.TryParse(key, out type)) throw new InvalidOperationException("SERVER_ITEM_DISPLAY_MAPPING: " + key);
            var template = TableManager.item.Get(type);
            return new ItemData { key = type, value = string.IsNullOrEmpty(value) ? null : value, count = count,
                category = template?.category ?? ItemCategoryType.Item, serverItemId = itemId };
        }
    }
}
