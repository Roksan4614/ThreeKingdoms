using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Data_Castle_Mission
{
    List<CastleMissionData> m_data;
    public IReadOnlyList<CastleMissionData> data => m_data;

    const string c_key = "pp_casltle_mission";


    int m_idxMission;

    CastleMissionLevelInfoData m_levelInfo;
    public CastleMissionLevelInfoData levelInfo => m_levelInfo;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_data = PPWorker.Get<List<CastleMissionData>>(c_key);

        //m_data = null; m_remainCount = 10;

        if (m_data == null)
        {
            m_idxMission = 1;
            m_data = new();
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

    public async UniTask StartMissionAsync(CastleMissionData _missionData, UnityAction<StatusType> _onComplete)
    {
        await UniTask.Yield();

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

        m_data.RemoveAt(idx);
        m_data.Add(data);
        AddNewMission(true, idx);

        m_levelInfo.missionCount = m_levelInfo.missionCount - 1;
        SaveLevelData();

        PopupManager.instance.AlertShow("미션_임무를_시작합니다.", -390);

        _onComplete(StatusType.Success);
    }

    public async UniTask CompleteMissionAsync(UnityAction<StatusType, int> _onComplete, params CastleMissionData[] _missionDatas)
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

        _onComplete(StatusType.Success, m_levelInfo.nowExp - prevExp);

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

        public long tickMission;
        [JsonProperty] int mission_count;

        public bool isUpgradable => nowExp >= maxExp;

        public System.DateTime dtMission => new System.DateTime(tickMission, System.DateTimeKind.Utc);
        public bool isDateChanged => Utils.GetUTC().Date > dtMission.Date;

        public int missionCount
        {
            get
            {
                CheckDate();
                return mission_count;
            }
            set
            {
                mission_count = value;
            }
        }

        void CheckDate()
        {
            if (isDateChanged)
            {
                mission_count = TableManager.castleEffect[CastleObjectType.Office].Get(level).mission_count.Value;
                tickMission = Utils.GetUTC().Ticks;
            }
        }
    }
}
