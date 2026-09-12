using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ThreeKingdoms.Client.Server
{
    // Client enum values are presentation/save identifiers, not s_position.idx.
    // All fourteen legacy keys still exist in the server table. The other three
    // are distinct positions, so none is aliased to an existing client value.
    public static class CharacterPositionCatalog
    {
        public static HeroPositionType FromServerKey(string key) => key switch
        {
            "prime_minister" => HeroPositionType.prime_minister,
            "grand_general" => HeroPositionType.grand_general,
            "grand_strategist" => HeroPositionType.grand_strategist,
            "director_of_the_secretariat" => HeroPositionType.director_of_the_secretariat,
            "palace_assistant_inspector" => HeroPositionType.palace_assistant_inspector,
            "general_of_the_vanguard" => HeroPositionType.general_of_the_vanguard,
            "general_of_the_left" => HeroPositionType.general_of_the_left,
            "general_of_the_mid" => HeroPositionType.general_of_the_mid,
            "general_of_the_right" => HeroPositionType.general_of_the_right,
            "general_of_the_rear" => HeroPositionType.general_of_the_rear,
            "military_sima" => HeroPositionType.military_sima,
            "suppresses_bandits" => HeroPositionType.suppresses_bandits,
            "vanquishes_rebels" => HeroPositionType.vanquishes_rebels,
            "the_standard" => HeroPositionType.the_standard,
            "general_of_the_cavalry" => HeroPositionType.general_of_the_cavalry,
            "chief_military_adviser" => HeroPositionType.chief_military_adviser,
            "dragon_fighter" => HeroPositionType.dragon_fighter,
            _ => throw new InvalidOperationException("Unknown server position key: " + key)
        };

        public static string ToServerKey(HeroPositionType type) => type switch
        {
            HeroPositionType.prime_minister => "prime_minister",
            HeroPositionType.grand_general => "grand_general",
            HeroPositionType.grand_strategist => "grand_strategist",
            HeroPositionType.director_of_the_secretariat => "director_of_the_secretariat",
            HeroPositionType.palace_assistant_inspector => "palace_assistant_inspector",
            HeroPositionType.general_of_the_vanguard => "general_of_the_vanguard",
            HeroPositionType.general_of_the_left => "general_of_the_left",
            HeroPositionType.general_of_the_mid => "general_of_the_mid",
            HeroPositionType.general_of_the_right => "general_of_the_right",
            HeroPositionType.general_of_the_rear => "general_of_the_rear",
            HeroPositionType.military_sima => "military_sima",
            HeroPositionType.suppresses_bandits => "suppresses_bandits",
            HeroPositionType.vanquishes_rebels => "vanquishes_rebels",
            HeroPositionType.the_standard => "the_standard",
            HeroPositionType.general_of_the_cavalry => "general_of_the_cavalry",
            HeroPositionType.chief_military_adviser => "chief_military_adviser",
            HeroPositionType.dragon_fighter => "dragon_fighter",
            _ => throw new InvalidOperationException("Invalid client position: " + type)
        };

        public static List<TableHeroPositionData> PresentationRows(List<TableHeroPositionData> legacyRows)
        {
            if (!GameServer.Enabled) return legacyRows;
            var rows = new List<TableHeroPositionData>();
            var seen = new HashSet<HeroPositionType>();
            foreach (var token in GameServer.TableRows("s_position"))
            {
                var type = FromServerKey((string)token["key"]);
                if (!seen.Add(type)) throw new InvalidOperationException("Duplicate server position: " + type);
                var row = (JObject)token.DeepClone();
                row["category"] = row["position_category"];
                rows.Add(row.ToObject<TableHeroPositionData>());
            }
            return rows;
        }

        public static string DisplayName(string key)
        {
            var lookup = "CHARACTER_POSITION_" + key.ToUpperInvariant();
            if (TableManager.stringTable?.Exists(lookup) == true) return TableManager.stringTable.GetString(lookup);
            // s_position has no display-name column. The planning PDF's example
            // list does not identify these three keys; these are UI translations
            // only, without aliases or changes to the server IDs/rules/effects.
            return key switch
            {
                "general_of_the_mid" => "중장군",
                "military_sima" => "군사마",
                "general_of_the_cavalry" => "기병장군",
                _ => key.Replace('_', ' ')
            };
        }
    }
}
