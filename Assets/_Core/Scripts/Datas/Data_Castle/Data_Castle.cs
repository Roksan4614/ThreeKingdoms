using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public partial class Data_Castle
{
    public Data_Castle_Mission mission { get; set; } = new();
    public Data_Castle_Building building { get; set; } = new();

    Dictionary<CastleObjectType, CastleData> m_db;
    IReadOnlyDictionary<CastleObjectType, CastleData> db => m_db;

    CancellationTokenSource m_cts;
    const string c_key = "pp_castle_data";

    public async UniTask InitializeAsync()
    {
        InitializeWallyAsync().Forget();

        List<UniTask> lstTask = new()
        {
            DoLoad_CastleDataAsync(),
            //DoLoad_ClaimDataAsync()
        };
        await UniTask.WhenAll(lstTask);
    }

    //async UniTask DoLoad_ClaimDataAsync()
    //{
    //    m_claimData = PPWorker.Get<CastleClaimData>("pp_castle_claim_data");

    //    await UniTask.Yield();
    //}

    async UniTask DoLoad_CastleDataAsync()
    {
        //PlayerPrefs.DeleteKey(c_key);

        var data = PPWorker.Get<List<CastleData>>(c_key);
        if (data == null)
        {
            m_db = new();
            var market = GetCaslteData(CastleObjectType.Market);
            market.tickClaim = Utils.GetUTC().Ticks;
            UpdateCastleData(market);

            var farm = GetCaslteData(CastleObjectType.Farm);
            farm.tickClaim = Utils.GetUTC().Ticks;
            UpdateCastleData(farm);
        }
        else
            m_db = data.ToDictionary(x => x.type, x => x);

        OnUpdateClaim(true);

        List<UniTask> lstTask = new()
        {
            mission.InitializeAsync(),
            building.InitializeAsync()
        };
        await UniTask.WhenAll(lstTask);

    }

    CancellationToken m_ctsToken;
    public void OnUpdateClaim(bool _isInit = false)
    {
        Release_CTS();
        m_cts = new();
        m_ctsToken = m_cts.Token;

        UpdateClaimAmountAsync(CastleObjectType.Farm).Forget();
        UpdateClaimAmountAsync(CastleObjectType.Market).Forget();
    }

    async UniTask UpdateClaimAmountAsync(CastleObjectType _objectType)
    {
        //await TutorialManager.WaitComplete(TutorialType.CASTLE_FINISHED, m_ctsToken);

        CastleData castleData = GetCaslteData(_objectType);
        var amount = GetAmountPerSecond(castleData);
        var maxAmount = GetMaxAmount(castleData);
        int maxAmount_Table = TableManager.castleEffect[_objectType].GetMaxAmount(castleData);
        maxAmount_Table = Mathf.RoundToInt(maxAmount_Table + maxAmount_Table * .45f);

        if (amount == 0)
            return;

        //if (castleData.totalAmount > maxAmount)
        //castleData.totalAmount = 1340;

        DateTime nextTime = default;
        if (castleData.tickClaim > 0)
        {
            DateTime lastClaimTime = new DateTime(castleData.tickClaim, DateTimeKind.Utc);
            TimeSpan elapsed = Utils.GetUTC() - lastClaimTime;

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
            nextTime = Utils.GetUTC();

        while (true)
        {
            UpdateCastleAmountData(castleData);

            if (castleData.totalAmount >= maxAmount)
            {
                if (castleData.totalAmount > maxAmount_Table)
                {
                    castleData.totalAmount = maxAmount_Table;
                    UpdateCastleAmountData(castleData);
                }

                await UniTask.WaitUntil(() => castleData.totalAmount < maxAmount, cancellationToken: m_ctsToken);
            }

            await UniTask.WaitUntil(() => nextTime <= Utils.GetUTC(), cancellationToken: m_ctsToken);

            nextTime = nextTime.AddSeconds(1f);
            castleData.totalAmount = Mathf.Min(maxAmount, castleData.totalAmount + amount);
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

        UpdateCastleData(newData, false);

        return GetCaslteData(_type);
    }

    public void UpdateCastleAmountData(CastleData _data)
    {
        var db = m_db[_data.type];
        db.totalAmount = _data.totalAmount;
        m_db[_data.type] = db;

        Signal.instance.UpdateFarmMarketData.Emit(db);
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

    public float GetAmountPerSecond(CastleData _data, bool _isWithProbity = true)
    {
        if (_data.type != CastleObjectType.Farm &&
            _data.type != CastleObjectType.Market)
        {
            return 0;
        }

        // 코어스탯 합산
        var tableCastle = TableManager.castle.GetCastleData(_data.type);
        int totalStat = GetTotalCoreStat(_data, tableCastle.coreStat[0]);

        // 현재 몇퍼인지 계산
        float percent = Mathf.Min(1f, totalStat / (float)_data.dbRise.value01);
        var perCeconds = TableManager.castleEffect[_data.type].GetAmountPerCeconds(_data);

        float result = perCeconds + perCeconds * (.45f * percent);
        return Mathf.Max(.1f, result);
    }

    /// <summary>
    /// 청렴도!!
    /// </summary>
    /// <returns></returns>
    public float GetGateProbityRate(bool _isJustPercent = false)
    {
        var gateData = GetCaslteData(CastleObjectType.Gate);

        // 최저 청렴도
        float minPercent = 0.5f;

        var totalLeaderShip = GetTotalCoreStat(gateData, CoreStatType.Leadership);
        var dbMaxLeaderShip = gateData.dbRise.value01;

        float percent = Mathf.Min(1f, totalLeaderShip / (float)dbMaxLeaderShip);

        if (_isJustPercent == true)
            return percent;

        return minPercent + minPercent * percent;
    }

    public float GetGatePublicOrderRate()
    {
        var gateData = GetCaslteData(CastleObjectType.Gate);

        var total = GetTotalCoreStat(gateData, CoreStatType.Strength);
        var dbMax = gateData.dbRise.value02;

        float percent = Mathf.Min(1f, total / (float)dbMax);

        return percent;
    }

    public float GetPalaceCharismaRate()
    {
        var palaceData = GetCaslteData(CastleObjectType.Palace);
        var totalCharisma = GetTotalCoreStat(palaceData, CoreStatType.Charisma);
        float percent = Mathf.Min(1f, totalCharisma / (float)palaceData.dbRise.orinValue01);
        return percent;
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

        var max = TableManager.castleEffect[_data.type].GetMaxAmount(_data);

        // 현재 몇퍼인지 계산
        float percent = Mathf.Min(1f, totalStat / (float)tableRiseData.value02);

        int result = Mathf.RoundToInt(max + max * (.45f * percent));
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

    public void GetSecondTimeStone(UnityAction<int, int> _callback)
    {
        var palace = GetCaslteData(CastleObjectType.Palace);
        var effectData = TableManager.castleEffect[CastleObjectType.Palace].Get(palace.level);
        _callback(effectData.time_stone_sec.Value, effectData.ad_reduce_min.Value);
    }

    public void Release()
    {
        SaveData();
        Release_CTS();
        Release_CTSWally();

        building.Release();
    }

    void Release_CTS()
        => m_cts = m_cts.ReleaseCTS();

    public string GetObjectName(CastleObjectType _type)
        => TableManager.stringTable.GetString("CASTLE_OBJECT_" + _type.ToString().ToUpper());

    public CastleObjectType GetHeroObjectType(string _heroKey)
    {
        for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
        {
            if (m_db.ContainsKey(i) && m_db[i].heroes.Contains(_heroKey))
                return i;
        }
        return CastleObjectType.NONE;
    }

    public async UniTask SetBatchHeroAsync(CastleData _castleData, UnityAction<StatusType> _onComplete)
    {
        await UniTask.NextFrame();

        // 기존 장수 삭제
        for (int i = 0; i < _castleData.heroes.Count; i++)
        {
            var heroKey = _castleData.heroes[i];
            var jobType = GetHeroObjectType(heroKey);

            if (jobType == CastleObjectType.NONE || jobType == _castleData.type)
                continue;

            var data = m_db[jobType];
            data.heroes.Remove(heroKey);

            Signal.instance.UpdateCastleHeroBatch.Emit(data);
        }

        {
            var data = m_db[_castleData.type];
            data.heroes = new();
            data.heroes.AddRange(_castleData.heroes);
            UpdateCastleData(data);

            Signal.instance.UpdateCastleHeroBatch.Emit(data);
        }

        building.UpdateBuildingUpgrade(CastleObjectType.NONE);
        OnUpdateClaim();

        _onComplete(StatusType.Success);
    }

    public async UniTask ClaimAsync(CastleObjectType _objectType, UnityAction<StatusType> _onComplete)
    {
        await UniTask.NextFrame();

        var castleData = m_db[_objectType];

        var probity = GetGateProbityRate();
        var count = (int)(castleData.totalAmount * probity);
        var itemType = _objectType == CastleObjectType.Market ? ItemType.Gold : ItemType.Rice;

        castleData.totalAmount = 0;
        castleData.todayClaimAmount = castleData.todayClaimAmount + count;
        castleData.tickClaim = Utils.GetUTC().Ticks;

        m_db[_objectType] = castleData;
        SaveData();
        OnUpdateClaim();

        PopupManager.instance.AlertShow($"{(itemType == ItemType.Gold ? "금화를" : "군량을")}_{count.AmountKMBT()}개_수령했습니다.");

        // SAVEDATA 재화 데이타 저장
        DataManager.userInfo.AddAsset(itemType, count, false, false);

        RewardWorker.instance.Run(CameraManager.posPointer,
            itemType, count, _isPopup: true, _isStartPunch: false);

        _onComplete(StatusType.Success);
    }

    public struct CastleData
    {
        public CastleObjectType type;
        public List<string> heroes;
        public int level;

        public long tickClaim;          // 회수한 시간
        public float totalAmount;       // 회수할 수 있는 총 재화량
        [JsonProperty] float today_claim_amount;

        public long tickUpgradeEnd;     // 업그레이드 시작
        public float remainUpgradeSeconds;  // 중단되서 멈췄을 때 남은 시간

        public TableCastleRiseData dbRise => TableManager.castleRise.GetRiseData(type, level);

        public bool isValidUpgrade
        {
            get
            {
                // 관아일 경우
                if (type == CastleObjectType.Office)
                {
                    return DataManager.castle.mission.levelInfo.isUpgradable;
                }
                else
                {
                    var dbCastle = TableManager.castle.GetCastleData(type);

                    for (int i = 0; i < dbCastle.coreStat.Length; i++)
                    {
                        var coreStat = dbCastle.coreStat[i];

                        if (coreStat == CoreStatType.NONE)
                            continue;

                        var total = DataManager.castle.GetTotalCoreStat(this, coreStat);
                        var max = type == CastleObjectType.Palace ? dbRise.orinValue01 : dbRise.maxCoreStat[i];

                        if (total < max)
                            return false;
                    }

                    if (type == CastleObjectType.Palace)
                    {
                        for (var type = CastleObjectType.NONE + 1; type < CastleObjectType.MAX; type++)
                        {
                            if (level != DataManager.castle.GetCaslteData(type).level)
                                return false;
                        }
                        return true;
                    }
                    else
                    {
                        var palaceLevel = DataManager.castle.GetCaslteData(CastleObjectType.Palace).level;
                        return level < palaceLevel;
                    }
                }
            }
        }

        public string name => TableManager.stringTable.GetString("CASTLE_OBJECT_" + type.ToString().ToUpper());
        public bool isDoingUpgrade => tickUpgradeEnd > 0;
        public DateTime dtUpgradeEnd
            => new DateTime(tickUpgradeEnd, DateTimeKind.Utc);

        public DateTime dtClaim => new DateTime(tickClaim, DateTimeKind.Utc);
        public bool isDateChanged => Utils.GetUTC().Date > dtClaim.Date;
        public int todayClaimAmount
        {
            get
            {
                CheckDate();
                return Mathf.FloorToInt(today_claim_amount);
            }
            set => today_claim_amount = value;

        }
        void CheckDate()
        {
            if (isDateChanged)
            {
                today_claim_amount = 0;
                tickClaim = Utils.GetUTC().Ticks;
            }
        }
    }
}

