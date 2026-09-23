using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;
using UnityEngine.Events;

public class QuestWorker
{
    static QuestWorker m_instance;
    public static QuestWorker instance => m_instance ??= new();
    public static void Release() => m_instance = null;

    QuestData m_data;
    const string c_key = "pp_quest_data";

    long m_tickCheck;
    const string c_key_tick = "pp_quest_data_check";

    public async UniTask InitializeAsync()
    {
        await UniTask.NextFrame();
        m_data = PPWorker.Get<QuestData>(c_key);
        if (m_data == null)
        {
            m_data = new();
            m_data.ResetData();

            SaveData();
        }

        m_tickCheck = PPWorker.Get<long>(c_key_tick);

        Signal.instance.DayChange.connect = SlotDayChange;
    }

    public long SaveReddotTick()
    {
        var tick = Utils.GetUTC().Ticks;
        PPWorker.Set(c_key_tick, tick);
        return tick;
    }

    public QuestInfoData GetQuestData(QuestCategoryType _category, QuestType _key)
        => m_data.GetQuestData(_category, _key);

    public void AddCount(QuestType _type)
    {
        for (var i = QuestCategoryType.NONE + 1; i < QuestCategoryType.MAX; i++)
        {
            var questData = m_data.GetQuestData(i, _type);

            if (questData.isComplete == false)
            {
                questData.AddCount();
                Signal.instance.Quest_UpdateStatus.Emit(questData);

                if (questData.isComplete == true)
                    Signal.instance.Quest_UpdateComplete.Emit(questData.type);
            }
        }

        SaveData();
    }

    void SlotDayChange()
    {
        var daily = TableManager.quest.GetQuestList(QuestCategoryType.daily);
        foreach (var q in daily)
            m_data.GetQuestData(QuestCategoryType.daily, q.key).ResetCount();
        m_data.rewardDaily.Clear();

        if (Utils.GetUTC().DayOfWeek == System.DayOfWeek.Monday)
        {
            var week = TableManager.quest.GetQuestList(QuestCategoryType.weekly);
            foreach (var q in week)
                m_data.GetQuestData(QuestCategoryType.weekly, q.key).ResetCount();
            m_data.rewardWeekly.Clear();
        }

        SaveData();

        Signal.instance.Quest_UpdateStatus.Emit(null);
        Signal.instance.Quest_UpdateComplete.Emit(QuestCategoryType.NONE);
    }

    public int GetCountComplete(QuestCategoryType _categoryType)
    {
        var db = _categoryType == QuestCategoryType.daily ? m_data.daily : m_data.weekly;

        int countComplete = 0;
        foreach (var q in db)
        {
            if (q.isComplete)
                countComplete++;
        }
        return countComplete;
    }

    public bool IsReceivedGaugeReward(QuestCategoryType _categoryType, int _targetValue)
        => (_categoryType == QuestCategoryType.daily ? m_data.rewardDaily : m_data.rewardWeekly).Contains(_targetValue);

    public async UniTask<bool> API_ReceiveGaugeReward(QuestCategoryType _categoryType, params int[] _targetValues)
    {
        int countComplete = GetCountComplete(_categoryType);
        var rewards = _categoryType == QuestCategoryType.daily ? m_data.rewardDaily : m_data.rewardWeekly;

        bool isUpdated = false;
        foreach (var targetValue in _targetValues)
        {
            if (countComplete >= targetValue && rewards.Contains(targetValue) == false)
            {
                rewards.Add(targetValue);
                isUpdated = true;
            }
        }

        if (isUpdated == true)
            SaveData();

        return isUpdated;
    }

    public async UniTask<bool> API_ReceiveReward(QuestInfoData _questData)
    {
        _questData.isReceiveReward = true;
        SaveData();

        return true;
    }

    public bool IsReddot(QuestCategoryType _categoryType = QuestCategoryType.NONE)
    {
        Queue<QuestCategoryType> queue = new();

        if (_categoryType == QuestCategoryType.NONE)
        {
            queue.Enqueue(QuestCategoryType.daily);
            queue.Enqueue(QuestCategoryType.weekly);
        }
        else
            queue.Enqueue(_categoryType);

        while (queue.Count > 0)
        {
            var category = queue.Dequeue();
            var db = category == QuestCategoryType.daily ? m_data.daily : m_data.weekly;
            foreach (var q in db)
            {
                if (q.tickComplete >= m_tickCheck)
                    return true;
            }
        }

        return false;
    }

    public bool HasNavigation(QuestType _type)
    {
        switch (_type)
        {
            case QuestType.TournamentPlay:
            case QuestType.RaidPlay:
            case QuestType.GachaProceed:
            case QuestType.RiceClaim:
            case QuestType.GoldClaim:
            case QuestType.OfficeDispatch:
            case QuestType.DailyDungeonPlay:
            case QuestType.ItemBuy:
            case QuestType.ItemUse:
            case QuestType.AdsWatch:
                return true;
            default:
                return false;
        }
    }

    public async UniTask<bool> NavigationAsync(QuestType _type, bool _isWidthModal = true)
    {
        if (_isWidthModal == true)
        {
            StatusType result = StatusType.Wait;

            if (_type == QuestType.AdsWatch)
                result = await PopupManager.instance.OpenModalAsync_Table("MODAL_AD_SHOW");
            else
                result = await PopupManager.instance.OpenModalAsync_Table("MODAL_MOVE_NAVI");

            if (result != StatusType.Success)
                return false;
        }

        switch (_type)
        {
            case QuestType.TournamentPlay:
                PopupManager.instance.OpenPopup(PopupType.LobbyTournament);
                break;
            case QuestType.RaidPlay:
                PopupManager.instance.OpenPopup(PopupType.LobbyBossRaid);
                break;
            case QuestType.GachaProceed:
                LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Summon);
                break;
            case QuestType.RiceClaim:
            case QuestType.GoldClaim:
                LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Castle);
                break;
            case QuestType.OfficeDispatch:
                LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Castle);
                Utils.AfterSecond(() => PopupManager.instance.OpenPopup(PopupType.Castle_Mission), .2f);
                break;
            case QuestType.DailyDungeonPlay:
                LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Boss);
                break;
            case QuestType.ItemBuy:
                LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Shop);
                break;
            case QuestType.ItemUse:
                PopupManager.instance.OpenPopup(PopupType.Inventory);
                break;
            //case QuestType.AdsWatch:
            //    AdsManager.instance.ShowAsync().Forget();
            //    return false;
            default:
                return false;
        }

        return true;
    }

    void SaveData() => PPWorker.Set(c_key, m_data);
}

public class QuestData
{
    public List<QuestInfoData> daily = new();
    public List<QuestInfoData> weekly = new();

    public List<int> rewardDaily = new();
    public List<int> rewardWeekly = new();

    public QuestInfoData GetQuestData(QuestCategoryType _type, QuestType _key)
    {
        var db = _type == QuestCategoryType.daily ? daily : weekly;
        var data = db.Find(x => x.key == _key);

        if (data == null)
        {
            data = new()
            {
                type = _type,
                key = _key,
            };
            db.Add(data);
        }

        return data;
    }

    public void ResetData()
    {
        for (var i = QuestCategoryType.NONE + 1; i < QuestCategoryType.MAX; i++)
        {
            var db = i == QuestCategoryType.daily ? daily : weekly;
            db.Clear();

            var table = TableManager.quest.GetQuestList(i);
            foreach (var d in table)
                db.Add(new()
                {
                    type = i,
                    key = d.key
                });
        }
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class QuestInfoData
{
    [JsonProperty] public QuestType key;
    [JsonProperty] public QuestCategoryType type;
    [JsonProperty] public int count;
    [JsonProperty] public bool isReceiveReward;
    [JsonProperty] public long tickComplete;

    TableQuestData m_data;
    public TableQuestData data => m_data ??= TableManager.quest.GetQuestData(type, key);

    public string name => TableManager.questString.GetString($"{Utils.ToSnakeCase(key.ToString()).ToUpper()}_NAME");

    public bool isComplete => count >= data.target_value;
    public void AddCount()
    {
        if (isComplete == true)
            return;

        count = Mathf.Min(count + 1, data.target_value);
        if (isComplete == true)
            tickComplete = Utils.GetUTC().Ticks;
    }
    public void ResetCount() => tickComplete = count = 0;

}
