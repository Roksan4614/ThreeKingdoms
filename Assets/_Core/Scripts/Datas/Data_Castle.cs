using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class Data_Castle
{
    public Data_Castle_Mission mission { get; set; } = new();

    Dictionary<CastleObjectType, CastleData> m_db;
    IReadOnlyDictionary<CastleObjectType, CastleData> db => m_db;

    CancellationTokenSource m_cts;
    const string c_key = "pp_castle_data";

    public async UniTask InitializeAsync()
    {
        List<UniTask> lstTask = new()
        {
            mission.InitializeAsync()
        };

        //PlayerPrefs.DeleteKey(c_key);

        var data = PPWorker.Get<List<CastleData>>(c_key);
        if (data == null)
            data = new();

        m_db = data.ToDictionary(x => x.type, x => x);

        OnUpdateClaim();

        await UniTask.WhenAll(lstTask);
    }

    public void OnUpdateClaim()
    {
        Release_CTS();
        m_cts = new();

        UpdateClaimAmountAsync(CastleObjectType.Farm).Forget();
        //UpdateClaimAmountAsync(CastleObjectType.Market).Forget();
    }

    async UniTask UpdateClaimAmountAsync(CastleObjectType _objectType)
    {
        CastleData castleData = GetCaslteData(_objectType);
        var amount = GetAmountPerSecond(castleData);
        var maxAmount = GetMaxAmount(castleData);
        int maxAmount_Table = TableManager.castleRise.GetRiseData(castleData.type, castleData.level).max_amount;
        maxAmount_Table = Mathf.RoundToInt(maxAmount_Table + maxAmount_Table * .45f);

        if (amount == 0)
            return;

        //if (castleData.totalAmount > maxAmount)
            //castleData.totalAmount = 1340;

        DateTime nextTime = default;
        if (castleData.tickClaim > 0)
        {
            DateTime lastClaimTime = new DateTime(castleData.tickClaim, DateTimeKind.Utc);
            TimeSpan elapsed = DateTime.UtcNow - lastClaimTime;

            if (elapsed.TotalSeconds >= 1)
            {
                // 소수점 버림으로 몇 초 지났는지 계산
                int passedSeconds = (int)elapsed.TotalSeconds;
                // lastClaimTime을 방금 계산한 시점까지 업데이트. 밀리초 유지
                lastClaimTime = lastClaimTime.AddSeconds(passedSeconds);
            }

            nextTime = lastClaimTime.AddSeconds(1f);
        }
        else
            nextTime = DateTime.UtcNow;

        while (true)
        {
            if (castleData.totalAmount >= maxAmount)
            {
                if (castleData.totalAmount > maxAmount_Table)
                {
                    castleData.totalAmount = maxAmount_Table;
                    UpdateCastleData(castleData);
                }

                Signal.instance.UpdateCastleData.Emit(castleData);

                await UniTask.WaitUntil(() => castleData.totalAmount < maxAmount, cancellationToken: m_cts.Token);
            }

            await UniTask.WaitUntil(() => nextTime <= DateTime.UtcNow, cancellationToken: m_cts.Token);

            nextTime = nextTime.AddSeconds(1f);
            castleData.totalAmount = Mathf.Min(maxAmount, castleData.totalAmount + amount);
            UpdateCastleData(castleData);

            Signal.instance.UpdateCastleData.Emit(castleData);
        }
    }

    public CastleData GetCaslteData(CastleObjectType _type)
    {
        if (m_db.ContainsKey(_type))
            return m_db[_type];

        CastleData newData = new()
        {
            type = _type,
            heroes = new(),
            level = 1
        };

        if (_type == CastleObjectType.Farm || _type == CastleObjectType.Market)
            newData.tickClaim = DateTime.UtcNow.Ticks;

        UpdateCastleData(newData);

        return GetCaslteData(_type);
    }

    public void UpdateCastleData(CastleData _data, bool _isSave = true)
    {
        if (m_db.ContainsKey(_data.type))
            m_db[_data.type] = _data;
        else
            m_db.Add(_data.type, _data);

        if (_isSave)
            SaveData();
    }

    public void SaveData()
    {
        PPWorker.Set(c_key, m_db.Values.ToList());
    }

    public float GetAmountPerSecond(CastleData _data)
    {
        if (_data.type != CastleObjectType.Farm &&
            _data.type != CastleObjectType.Market)
        {
            return 0;
        }

        // 코어스탯 합산
        var tableCastle = TableManager.castle.GetCastleData(_data.type);
        int totalStat = GetTotalCoreStat(_data, tableCastle.coreStat[0]);

        var tableRiseData = TableManager.castleRise.GetRiseData(_data.type, _data.level);

        // 현재 몇퍼인지 계산
        float percent = Mathf.Min(1f, totalStat / (float)tableRiseData.value_01);

        // 청렴도
        float probity = GetProbityRate();

        float result = tableRiseData.rate_per_second + tableRiseData.rate_per_second * (.45f * percent);
        return result == 0 ? 0 : Mathf.Max(1, result * probity);
    }

    public float GetProbityRate()
    {
        var gateData = GetCaslteData(CastleObjectType.Gate);

        float minPercent = 0.5f;
        var totalLeaderShip = GetTotalCoreStat(gateData, CoreStatType.Leadership);
        var dbMaxLeaderShip = TableManager.castleRise.GetRiseData(CastleObjectType.Gate, m_db[CastleObjectType.Gate].level).value_01;

        float percent = Mathf.Min(1f, totalLeaderShip / (float)dbMaxLeaderShip);

        return minPercent + minPercent * percent;
    }

    public int GetMaxAmount(CastleData _data)
    {
        if (_data.type != CastleObjectType.Farm &&
            _data.type != CastleObjectType.Market)
        {
            return 0;
        }

        // 코어스탯 합산
        var tableCastle = TableManager.castle.GetCastleData(_data.type);
        int totalStat = GetTotalCoreStat(_data, tableCastle.coreStat[1]);
        var tableRiseData = TableManager.castleRise.GetRiseData(_data.type, _data.level);

        // 현재 몇퍼인지 계산
        float percent = Mathf.Min(1f, totalStat / (float)tableRiseData.value_02);

        int result = Mathf.RoundToInt(tableRiseData.max_amount + tableRiseData.max_amount * (.45f * percent));
        return result;
    }

    public int GetTotalCoreStat(CastleData _data, CoreStatType _coreStatType)
    {
        int totalStat = 0;
        for (int i = 0; i < _data.heroes.Count; i++)
        {
            var heroData = DataManager.userInfo.GetHeroInfoData(_data.heroes[i]);

            totalStat += heroData.resultCoreStat[_coreStatType];
        }
        return totalStat;
    }


    public void Release()
    {
        SaveData();
        Release_CTS();
    }

    void Release_CTS()
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }
    }

    public string GetObjectName(CastleObjectType _type)
        => TableManager.stringTable.GetString("CASTLE_OBJECT_" + _type.ToString().ToUpper());

    public CastleObjectType GetJobObjectType(string _heroKey)
    {
        for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
        {
            if (m_db.ContainsKey(i) && m_db[i].heroes.Contains(_heroKey))
                return i;
        }
        return CastleObjectType.NONE;
    }

    public struct CastleData
    {
        public CastleObjectType type;
        public List<string> heroes;
        public int level;
        public long tickClaim;      //회수한 시간 tick
        public float totalAmount;     //회수할 수 있는 총 재화량
    }
}

