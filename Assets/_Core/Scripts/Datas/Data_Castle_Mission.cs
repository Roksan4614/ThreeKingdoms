using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_Castle_Mission
{
    List<CastleMissionData> m_data;
    public IReadOnlyList<CastleMissionData> data => m_data;

    int m_remainCount;
    public int remainCount => m_remainCount;

    const string c_key = "pp_casltle_mission";

    int m_idxMission;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_remainCount = PPWorker.Get<int>(c_key + "_count");
        m_data = PPWorker.Get<List<CastleMissionData>>(c_key);

        m_data = null; m_remainCount = 10;

        if (m_data == null)
        {
            m_idxMission = 1;
            m_data = new();
            m_remainCount = 10;
            var dbTable = TableManager.castleMisson.list.OrderBy(x => Random.value).ToArray();

            for (int i = 0; i < 3; i++)
            {
                var grade = GradeType.NONE + 1 + Random.Range(0, (int)GradeType.MAX);
                CastleMissionData newData = new()
                {
                    idx = m_idxMission++,
                    key = dbTable[i].key,
                    grade = grade,
                    exp = 100 * ((int)grade + 1),
                    rewardList = new(),
                    heroes = new()
                };

                int max = ((int)newData.grade + 1) * 3;
                for (int j = 0; j < max; j++)
                {
                    newData.rewardList.Add(new()
                    {
                        key = ItemType.Gold,
                        count = Random.Range(10, 20) * ((int)grade + 1)
                    });
                }

                m_data.Add(newData);
                SaveData();
            }
        }
        else
            m_idxMission = m_data.Last().idx + 1;
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
            AddNewMission(false);

        SaveData();
    }

    public void AddNewMission(bool _isAutoSave)
    {
        var a = TableManager.castleMisson.list.OrderBy(x => Random.value);
        var b = a.Where(x => m_data.Any(x => x.key.Equals(x.key) == false));
        var c = b.FirstOrDefault();

        var newMission = TableManager.castleMisson.list.OrderBy(x => Random.value).Where(x => m_data.Any(x => x.key.Equals(x.key) == false)).FirstOrDefault();

        if (newMission.isActive == false)
            newMission = TableManager.castleMisson.list.OrderBy(x => Random.value).FirstOrDefault();

        var grade = GradeType.NONE + 1 + Random.Range(0, (int)GradeType.MAX);
        CastleMissionData newData = new()
        {
            idx = m_idxMission++,
            key = newMission.key,
            grade = grade,
            exp = 100 * ((int)grade + 1),
            rewardList = new(),
            heroes = new()
        };

        int max = ((int)newData.grade + 1) * 3;
        for (int j = 0; j < max; j++)
        {
            newData.rewardList.Add(new()
            {
                key = ItemType.Gold,
                count = Random.Range(10, 20) * ((int)grade + 1)
            });
        }

        m_data.Add(newData);

        if (_isAutoSave)
            SaveData();
    }

    public void StartMission(CastleMissionData _missionData)
    {
        _missionData.tickStart = System.DateTime.UtcNow.Ticks;
        _missionData.tickEnd = System.DateTime.UtcNow.AddSeconds(((int)_missionData.grade + 3) * 10).Ticks;

        m_remainCount--;
        UpdateMission(_missionData, false);
        AddNewMission(true);
    }

    public void RemoveMission(int _idx, bool _isForceSave = true)
    {
        var idx = m_data.FindIndex(x => x.idx == _idx);
        if (idx == -1) return;
        m_data.RemoveAt(idx);

        if (_isForceSave)
            SaveData();
    }

    public void UpdateMission(CastleMissionData _missionData, bool _isForceUpdate = true)
    {
        var idx = m_data.FindIndex(x => x.idx == _missionData.idx);

        if (idx == -1) return;
        m_data[idx] = _missionData;

        if (_isForceUpdate)
            SaveData();
    }

    public void SaveData()
    {
        PPWorker.Set(c_key + "_count", m_remainCount);
        PPWorker.Set(c_key, m_data);
    }

    public struct CastleMissionData
    {
        public int idx;
        public string key;
        public List<string> heroes;
        public GradeType grade;
        public long tickStart;
        public long tickEnd;
        public long exp;
        public List<TableItemData> rewardList;

        TableCastleMissionData m_dbData;
        public TableCastleMissionData dbData
        {
            get
            {
                if (m_dbData.isActive == false)
                    m_dbData = TableManager.castleMisson.Get(key);
                return m_dbData;
            }
        }

        public bool isActive => key.IsActive();
        //TODO : stringtable 에서 가져와야 해.
        public string missionName => $"[{TableManager.stringTable.GetString($"GRADE_{grade.ToString().ToUpper()}")}] {key}";
    }
}
