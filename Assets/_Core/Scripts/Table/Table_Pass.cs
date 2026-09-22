using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

namespace Rev9.Pass
{

    public class Table_PassReward : BaseTable<int, TablePassRewardData>
    {
        public Table_PassReward(List<TablePassRewardData> _table) : base(_table) { }

        public TablePassRewardData GetRewardData(int _level)
            => m_list.Find(x => x.level == _level);

        public ItemData GetRewardItem(int _level, bool _isPaid)
        {
            var data = GetRewardData(_level);
            return _isPaid ? data.itemDataPaid : data.itemData;
        }
    }

    public class TablePassRewardData
    {
        public int level;
        public string reward_key;
        public int reward_count;
        public string paid_reward_key;
        public int paid_reward_count;
        public int exp;

        ItemData m_itemData;
        public ItemData itemData
            => m_itemData ??= TableManager.item.GetItemData(reward_key, reward_count);

        ItemData m_itemDataPaid;
        public ItemData itemDataPaid
            => m_itemDataPaid ??= TableManager.item.GetItemData(paid_reward_key, paid_reward_count);
    }

    public class Table_PassQuest : BaseTable<int, TablePassQuestData>
    {
        public Table_PassQuest(List<TablePassQuestData> _table) : base(_table) { }

        public int GetCount(TablePassQuestData _data)
            => m_list.Find(x => x.key == _data.key && x.type == _data.type)?.count ?? 0;
        public TablePassQuestData GetRandomQuest(bool _isSeason)
        {
            TablePassQuestData result = new();

            var type = _isSeason ? QuestDateType.Season : QuestDateType.Daily;
            var questData = m_list.FindAll(x => x.type == type).RandomFirst();

            result.key = questData.key;
            result.type = questData.type;
            result.exp = questData.exp;
            result.is_character = 1;
            result.value = "GuanYu";

            return result;
        }
    }

    public class TablePassQuestData
    {
        public QuestType key;
        public QuestDateType type;
        public int count;
        public int exp;
        public int is_character;
        public int is_country;
        public int is_class;

        //custom
        public string value { get; set; }

        public string name => TableManager.questString.GetString($"{Utils.ToSnakeCase(key.ToString()).ToUpper()}_NAME");

        string m_valueHero;
        public string valueHero
        {
            get
            {
                if (m_valueHero == null)
                    m_valueHero = is_character > 0 ? value : "";
                return m_valueHero;
            }
        }

        RegionType m_valueRegion = RegionType.NONE - 1;
        public RegionType valueRegion
        {
            get
            {
                if (m_valueRegion == RegionType.NONE - 1)
                {
                    m_valueRegion = RegionType.NONE;
                    if (is_country > 1)
                    {
                        for (var i = RegionType.NONE + 1; i < RegionType.MAX; i++)
                        {
                            if (i.ToString().ToLower() == value.ToLower())
                            {
                                m_valueRegion = i;
                                break;
                            }
                        }
                    }
                }
                return m_valueRegion;
            }
        }

        HeroClassType m_valueClass = HeroClassType.NONE - 1;
        public HeroClassType valueClass
        {
            get
            {
                if (m_valueClass == HeroClassType.NONE - 1)
                {
                    m_valueClass = HeroClassType.NONE;
                    if (is_class > 1)
                    {
                        for (var i = HeroClassType.NONE + 1; i < HeroClassType.MAX; i++)
                        {
                            if (i.ToString().ToLower() == value.ToLower())
                            {
                                m_valueClass = i;
                                break;
                            }
                        }
                    }
                }
                return m_valueClass;
            }
        }

        public string resultValueName
        {
            get
            {
                if (is_class == 0 && is_country == 0 && is_character == 0) return "";

                if (is_character > 0)
                    return TableManager.stringHero.GetName(value);
                else if (is_country > 0)
                    return TableManager.stringTable.GetRegionType(valueRegion, true);
                else
                    return TableManager.stringHero.GetClassType(valueClass);
            }
        }

        public string GetDesc(string _value)
            => TableManager.questString.GetStringFormat($"{Utils.ToSnakeCase(key.ToString()).ToUpper()}_DESC", _value);
    }
}