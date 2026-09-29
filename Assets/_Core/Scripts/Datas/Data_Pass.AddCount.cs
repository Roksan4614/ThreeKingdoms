using ThreeKingdoms.Shared.Enums;
using UnityEngine;

namespace Rev9.Pass
{
    public partial class Data_Pass
    {
        public void AddCount_WithOfficers(QuestType _questType)
        {
            bool isUpdated = false;
            foreach (var q in m_data.quests)
            {
                if (q.tableData.key == _questType)
                {
                    if (TeamManager.instance.HasHero(q.tableData.valueHero) ||
                        TeamManager.instance.HasHeroClass(q.tableData.valueClass) ||
                        TeamManager.instance.HasHeroRegion(q.tableData.valueRegion))
                    {
                        isUpdated = true;
                        q.tableData.count++;
                    }
                }
            }

            if (isUpdated == true)
            {
                SaveData();
                Signal.instance.Pass_UpdateQuest.Emit(QuestType.EnemyKill);
            }
        }
        public void AddCount_EnemyKill()
            => AddCount_WithOfficers(QuestType.EnemyKill);
        public void AddCount_TournamentPlay()
            => AddCount_WithOfficers(QuestType.TournamentPlay);
        public void AddCount_RaidPlay()
            => AddCount_WithOfficers(QuestType.RaidPlay);
        public void AddCount_OfficeDispatch()
            => AddCount_WithOfficers(QuestType.OfficeDispatch);
        public void AddCount_DailyDungeonPlay()
            => AddCount_WithOfficers(QuestType.DailyDungeonPlay);
    }
}