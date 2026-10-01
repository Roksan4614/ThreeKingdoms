using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;

public class Data_HeroPosition : MonoBehaviour
{
    //public Dictionary<PositionCategory, List<HeroPositionData>> data { get; private set; } = new();

    public List<HeroPositionData> data { get; private set; }
    const string c_key = "pp_hero_position";

    void SaveData() => PPWorker.Set(c_key, data);

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        data = PPWorker.Get<List<HeroPositionData>>(c_key);

        if (data == null)
        {
            data = new();
            SaveData();
        }
    }

    public HeroPositionData GetHeroPositionData(PositionType _type)
    {
        return data.Find(x => x.type == _type);
    }

    public HeroPositionData GetHeroPosition(string _heroKey)
    {
        return data.Find(x => x.heroKey == _heroKey);
    }

    public async UniTask<bool> API_BindPosition(string _heroKey, PositionType _type)
    {
        await UniTask.NextFrame();

        // 같은 장수를 다른곳에 배치한곳이 있으면 삭제해주자.
        var idxPrev = data.FindIndex(x => x.heroKey == _heroKey);
        if (idxPrev > -1)
        {
            bool isDelete = data[idxPrev].type == _type;
            data.RemoveAt(idxPrev);

            if (isDelete == true)
                return true;
        }

        // 해당 직책에 다른 유저가 이미 있다면 해지해 주자.
        idxPrev = data.FindIndex(x => x.type == _type);
        if (idxPrev > -1)
        {
            var result = await PopupManager.instance.OpenModalAsync_Table("MODAL_OUT_BEFORE_HERO");// "기존_장수를_해임하겠습니까 ?");

            if (result == StatusType.Success)
            {
                DataManager.userInfo.ResetResultStat(data[idxPrev].heroKey);
                data.RemoveAt(idxPrev);
            }
            else
                return false;
        }

        data.Add(new()
        {
            heroKey = _heroKey,
            type = _type
        });

        DataManager.userInfo.ResetResultStat(_heroKey);
        SaveData();

        return true;
    }

    //void SetBindHero(PositionCategory _category, PositionType _key, string _heroKey)
    //{
    //    if (data.ContainsKey(_category) == false)
    //    {
    //        IngameLog.Add("HeroPosition Cant Find: " + _category);
    //        return;
    //    }

    //    int idx = data[_category].FindIndex(x => x.heroKey.Equals(_heroKey));
    //    if (idx > -1)
    //    {
    //        var d = data[_category][idx];
    //        d.heroKey = null;
    //        data[_category][idx] = d;

    //        //TODO 보너스 스탯에서 차감해줘야 해
    //    }

    //    idx = data[_category].FindIndex(x => x.key == _key);
    //    {
    //        var d = data[_category][idx];
    //        d.heroKey = _heroKey;
    //        data[_category][idx] = d;

    //        //TODO 보너스 스탯에서 증가시켜줘야 해
    //    }
    //}
}

[JsonObject(MemberSerialization.OptIn)]
public class HeroPositionData
{
    [JsonProperty] public PositionType type;
    [JsonProperty] public string heroKey;

    TableHeroPositionData m_positionData;
    public TableHeroPositionData positionData => m_positionData ??= TableManager.heroPosition.GetData(type);
}

//public class TableHeroPositionData
//{
//    public PositionCategory category;
//    public PositionType position;
//    public BattleStatType battleStatType;
//    public float value;
//}


//[System.Serializable]
//public class HeroPositionData
//{
//    public PositionType key;
//    public Dictionary<BattleStatType, float> bonusStat;

//    //custom
//    string m_heroKey;
//    public string heroKey
//    {
//        get => m_heroKey;
//        set => m_heroKey = value;
//    }

//    public string name => TableManager.stringTable.GetPositionType(key);
//    public string stringAttribute
//    {
//        get
//        {
//            string result = "";

//            int idx = 0;
//            foreach (var s in bonusStat)
//            {
//                if (idx > 0)
//                    result += "\n";

//                result += $"{s.Key} +{s.Value:0.00}%";
//                idx++;
//            }
//            return result;
//        }
//    }
//}