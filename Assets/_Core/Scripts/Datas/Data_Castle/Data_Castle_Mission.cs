using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Data_Castle_Mission
{
    List<CastleMissionData> m_data;
    public IReadOnlyList<CastleMissionData> data => m_data;

    int m_remainCount;
    public int remainCount => m_remainCount;

    const string c_key = "pp_casltle_mission";

    int m_idxMission;

    CastleMissionLevelInfoData m_levelInfo;
    public CastleMissionLevelInfoData levelInfo => m_levelInfo;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_remainCount = PPWorker.Get<int>(c_key + "_count");
        m_data = PPWorker.Get<List<CastleMissionData>>(c_key);

        //m_data = null; m_remainCount = 10;

        if (m_data == null)
        {
            m_idxMission = 1;
            m_data = new();
            m_remainCount = 10;
            var dbTable = TableManager.castleMisson.list.OrderBy(x => Random.value).ToArray();

            for (int i = 0; i < 3; i++)
            {
                var grade = GradeType.NONE + 1 + Random.Range(0, 3) * 2;
                CastleMissionData newData = new()
                {
                    idx = m_idxMission++,
                    key = dbTable[UnityEngine.Random.Range(0, dbTable.Length)].key,
                    grade = grade,
                    heroes = new()
                };

                m_data.Add(newData);
                SaveData();
            }
        }
        else
            m_idxMission = m_data.OrderBy(x => x.idx).Last().idx + 1;

        //PlayerPrefs.DeleteKey(c_key + "_levelinfo");

        if (PPWorker.HasKey(c_key + "_levelinfo"))
            m_levelInfo = PPWorker.Get<CastleMissionLevelInfoData>(c_key + "_levelinfo");
        else
        {
            m_levelInfo = new()
            {
                level = 1,
                nowExp = 0,
                maxExp = TableManager.castleOfficeLevel.GetLevelInfo(2).req_xp
            };
            SaveLevelData();
        }
    }

    public void RefreshMission()
    {
        while (true)
        {
            var m = m_data.Find(x => x.tickStart == 0);
            if (m.isActive == false)
                break;

            m_data.Remove(m);
        }

        for (int i = 0; i < 3; i++)
            AddNewMission(false, i);

        SaveData();
    }

    public void AddNewMission(bool _isAutoSave, int _prevNumber)
    {
        var newMission = TableManager.castleMisson.list.Where(x => m_data.Any(x => x.key.Equals(x.key) == false)).OrderBy(x => Random.value).FirstOrDefault();

        if (newMission.isActive == false)
            newMission = TableManager.castleMisson.list.OrderBy(x => Random.value).FirstOrDefault();

        var grade = GradeType.NONE + 1 + Random.Range(0, 3) * 2;
        CastleMissionData newData = new()
        {
            idx = m_idxMission++,
            key = newMission.key,
            grade = grade,
            heroes = new()
        };

        m_data.Insert(_prevNumber, newData);

        if (_isAutoSave)
            SaveData();
    }

    public void StartMission(CastleMissionData _missionData)
    {
        var idx = m_data.FindIndex(x => x.idx == _missionData.idx);
        var data = m_data[idx];

        data.tickStart = Utils.GetUTC().Ticks;
        data.percentStat = _missionData.percentStat;
        data.heroes.AddRange(_missionData.heroes);
        var sec = data.dbGradeData.durationSeconds;
#if UNITY_EDITOR
        data.tickEnd = Utils.GetUTC().AddSeconds(((int)_missionData.grade + 3) * 5).Ticks;
#else
        data.tickEnd = Utils.GetUTC().AddSeconds(sec).Ticks;
#endif

        m_remainCount--;

        m_data.RemoveAt(idx);
        m_data.Add(data);
        AddNewMission(true, idx);
    }

    public async UniTask CompleteMissionAsync(UnityAction _onComplete, params CastleMissionData[] _missionDatas)
    {
        // 모두 받기
        if (_missionDatas.Length == 0)
            _missionDatas = m_data.Where(x => x.tickEnd > 0 && x.tickEnd < Utils.GetUTC().Ticks).ToArray();

        int prevExp = m_levelInfo.nowExp;

        for (int i = 0; i < _missionDatas.Length; i++)
        {
            m_levelInfo.nowExp += _missionDatas[i].dbGradeData.missionXp;
            RemoveMission(_missionDatas[i].idx, false);
        }

        SaveLevelData();
        SaveData();

        if (prevExp < m_levelInfo.maxExp && m_levelInfo.nowExp >= m_levelInfo.maxExp)
            PopupManager.instance.AlertShow("관아 업그레이드 준비완료!");

        _onComplete();

        // 보상 연출 해주자
        Dictionary<ItemType, TableItemData> dbRewards = new();
        foreach (var m in _missionDatas)
        {
            var reward = TableManager.castleMissonReward.GetReward(m).Where(x => x.unlock_pct <= m.percentStat).ToList();
            foreach (var r in reward)
            {
                if (dbRewards.ContainsKey(r.reward_key))
                {
                    var db = dbRewards[r.reward_key];
                    db.count += UnityEngine.Random.Range(r.reward_min, r.reward_max + 1);
                }
                else
                {
                    dbRewards.Add(r.reward_key, new()
                    {
                        key = r.reward_key,
                        value = r.reward_value,
                        count = UnityEngine.Random.Range(r.reward_min, r.reward_max + 1)
                    });
                }
            }
        }

        var rewards = dbRewards.Values.Select(x => new RewardWorker.RewardItemData(x.key, x.count)).ToList();
        foreach (var r in rewards)
            RewardWorker.instance.Run(CameraManager.posPointer, r.itemType, r.count, _isCanvas: true);


        await UniTask.Yield();
    }

    void RemoveMission(int _idx, bool _isForceSave = true)
    {
        var idx = m_data.FindIndex(x => x.idx == _idx);
        if (idx == -1) return;
        m_data.RemoveAt(idx);

        if (_isForceSave)
            SaveData();
    }

    public int GetMissionIdxBatchHero(string _heroKey)
    {
        var d = m_data.Find(x => x.heroes.Contains(_heroKey));

        return d.isActive ? d.idx : -1;
    }

    public int GetTotalCoreStat(CastleMissionData _missionData)
    {
        var coreStat = _missionData.dbData.statType;

        int totalStat = 0;
        for (int i = 0; i < _missionData.heroes.Count; i++)
        {
            var heroData = DataManager.userInfo.GetHeroInfoData(_missionData.heroes[i]);
            totalStat += heroData.resultCoreStat[coreStat];
        }
        return totalStat;
    }




    public void SetUpgradeOffice()
    {
        var nextLevelInfo = TableManager.castleOfficeLevel.GetLevelInfo(m_levelInfo.level + 1);

        m_levelInfo.level++;
        m_levelInfo.nowExp -= m_levelInfo.maxExp;
        m_levelInfo.maxExp = nextLevelInfo.level == 0 ? -1 : nextLevelInfo.req_xp;
        SaveLevelData();
    }

    public void SaveData()
    {
        PPWorker.Set(c_key + "_count", m_remainCount);
        PPWorker.Set(c_key, m_data);
    }

    public void SaveLevelData()
    {
        PPWorker.Set(c_key + "_levelinfo", m_levelInfo);
    }

    public struct CastleMissionData
    {
        public int idx;
        public string key;
        public List<string> heroes;
        public GradeType grade;
        public long tickStart;
        public long tickEnd;
        public float percentStat;

        public TableCastleMissionData dbData
            => TableManager.castleMisson.Get(key);

        public TableCastleMissionGradeData dbGradeData
            => TableManager.castleMissonGrade.Get(grade);

        //public IReadOnlyList<TableCastleMissionRewardData> dbRewardData
        //    => TableManager.castleMissonReward.GetReward(this);

        public bool isActive => key.IsActive();
        //TODO : stringtable 에서 가져와야 해.
        string missionName => TableManager.stringMission.GetString(key.ToUpper() + "_TITLE");
        public string missionNameStat => $"[{TableManager.stringTable.GetString($"CORESTAT_{dbData.statType.ToString().ToUpper()}")}] {missionName}";
        public string gradeName => TableManager.stringMission.GetString($"GRADETYPE_{grade.ToString().ToUpper()}");

        public int coreStatMax => dbGradeData.reqStatValue;
        public int xp => dbGradeData.missionXp;
        public int durationSeconds => dbGradeData.durationSeconds;
    }


    public struct CastleMissionLevelInfoData
    {
        public int level;
        public int nowExp;
        public int maxExp;

        public bool isUpgradable => nowExp >= maxExp;
    }
}
