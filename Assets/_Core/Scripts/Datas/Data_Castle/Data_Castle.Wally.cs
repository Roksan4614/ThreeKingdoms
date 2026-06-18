using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public partial class Data_Castle
{
    CancellationTokenSource m_ctsWally;
    const string c_keyWally = "pp_castle_wally";

    CastleWallyData m_wallyData;
    public CastleWallyData wallyData => m_wallyData;

    public bool m_isUISpawn;

    void Release_CTSWally()
        => m_ctsWally = m_ctsWally.Release();

    public void SetWallyUISpawn(bool _isSpawn)
        => m_isUISpawn = _isSpawn;

    async UniTask InitializeWallyAsync(bool _isForceSpawn = false)
    {
        Release_CTSWally();
        m_ctsWally = new();
        var token = m_ctsWally.Token;

        await TutorialManager.WaitComplete(TutorialType.CASTLE_FINISHED, token);

        m_wallyData = PPWorker.Get<CastleWallyData>(c_keyWally);

        while (true)
        {
            var ts = m_wallyData.dtNextCheck - Utils.GetUTC();

            if (m_wallyData.tickSpawn == 0 && ts.TotalSeconds > 0)
            {
                await UniTask.WaitForSeconds((float)ts.TotalSeconds, cancellationToken: token);

                // 도적 등장 확률 계산해주자
                var percent = GetGatePublicOrderRate();
                if (UnityEngine.Random.value <= 1 - percent)
                {
                    var utc = Utils.GetUTC();

                    PopupManager.instance.AlertShow("영지에_도적이_출몰했습니다!");

                    var gateData = GetCaslteData(CastleObjectType.Gate);
                    var durationNPCSec = TableManager.castleEffect[CastleObjectType.Gate].Get(gateData.level).npc_duration_sec.Value;
                    float percentSteel = 0.3f;

#if UNITY_EDITOR
                    durationNPCSec = 30;
                    percentSteel = 0.1f;
#endif

                    //농지 최대치 뺏어오자
                    {
                        var farmData = GetCaslteData(CastleObjectType.Farm);
                        var steelAmount = Mathf.Min(farmData.totalAmount, GetMaxAmount(farmData) * percentSteel);
                        farmData.totalAmount -= steelAmount;

                        m_wallyData.steelAmount_Rice = steelAmount;
                        UpdateCastleAmountData(farmData);
                    }
                    //시장 최대치 뺏어오자
                    {
                        var marketData = GetCaslteData(CastleObjectType.Market);
                        var steelAmount = Mathf.Min(marketData.totalAmount, GetMaxAmount(marketData) * percentSteel);
                        marketData.totalAmount -= steelAmount;

                        m_wallyData.steelAmount_Gold = steelAmount;
                        UpdateCastleAmountData(marketData);
                    }

                    OnUpdateClaim();

                    m_wallyData.tickSpawn = utc.Ticks;
                    m_wallyData.tickEndSpawn = utc.AddSeconds(durationNPCSec).Ticks;
                    Signal.instance.CastleWally_Spawn.Emit();
                    SaveData_Wally();
                }
            }

            if (m_wallyData.tickSpawn > 0)
            {
                var utcEndSpawn = new DateTime(m_wallyData.tickEndSpawn, DateTimeKind.Utc);
                var dtEnd = DateTime.Now.AddSeconds((utcEndSpawn - Utils.GetUTC()).TotalSeconds);

                while (true)
                {
                    if (m_wallyData.tickSpawn == 0)
                        break;

                    if (dtEnd < DateTime.Now)
                    {
                        await UniTask.WaitUntil(() => m_isUISpawn == false, cancellationToken: token);

                        m_wallyData.tickSpawn = m_wallyData.tickEndSpawn = 0;
                        PopupManager.instance.AlertShow("영지에서_도적이_도망쳤습니다!");
                        Signal.instance.CastleWally_Failed.Emit();

                        break;
                    }

                    await UniTask.WaitForEndOfFrame(cancellationToken: token);
                }
            }

#if UNITY_EDITOR
            m_wallyData.tickNextCheck = Utils.GetUTC().AddSeconds(10).Ticks;
#else
            m_wallyData.tickNextCheck = Utils.GetUTC().AddSeconds(600).Ticks;
#endif

            SaveData_Wally();
        }
    }

    public void HitWally()
    {
        // TODO 보상을 주자

        var dtStart = new DateTime(m_wallyData.tickSpawn, DateTimeKind.Utc);
        var dtEnd = new DateTime(m_wallyData.tickEndSpawn, DateTimeKind.Utc);

        var totalSec = (dtEnd - dtStart).TotalSeconds;
        var remainSec = (dtEnd - Utils.GetUTC()).TotalSeconds;
        var percent = 1 - (remainSec / totalSec);

        var rewardGold = Mathf.RoundToInt(m_wallyData.steelAmount_Gold * (float)percent);
        var rewardRice = Mathf.RoundToInt(m_wallyData.steelAmount_Rice * (float)percent);

        RewardWorker.instance.AddAsset(rewardGold, rewardRice);

        m_wallyData.steelAmount_Gold = m_wallyData.steelAmount_Rice = 0;
        m_wallyData.tickSpawn = m_wallyData.tickEndSpawn = 0;
        PopupManager.instance.AlertShow("도적을_잡았습니다!");
    }

    void SaveData_Wally()
    {
        PPWorker.Set(c_keyWally, m_wallyData);
    }

    public struct CastleWallyData
    {
        public long tickNextCheck;
        public long tickSpawn;
        public long tickEndSpawn;

        public float steelAmount_Gold;
        public float steelAmount_Rice;

        public bool isSpawn => tickSpawn > 0;
        public DateTime dtNextCheck => new DateTime(tickNextCheck, DateTimeKind.Utc);
    }
}
