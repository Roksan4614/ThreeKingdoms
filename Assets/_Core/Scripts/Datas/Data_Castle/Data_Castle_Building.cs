using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Data_Castle_Building
{
    Dictionary<CastleObjectType, CancellationTokenSource> m_cts = new();

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        UpdateBuildingUpgrade(CastleObjectType.NONE);
    }

    public void UpdateBuildingUpgrade(string _heroKey)
    {
        var objectType = DataManager.castle.GetHeroObjectType(_heroKey);
        if (objectType != CastleObjectType.NONE)
            UpdateBuildingUpgrade(objectType);
    }

    public void UpdateBuildingUpgrade(CastleObjectType _objectType)
        => UpdateBuindingUpgradeAsync(_objectType).Forget();

    async UniTask UpdateBuindingUpgradeAsync(CastleObjectType _objectType)
    {
        if (_objectType == CastleObjectType.NONE)
        {
            for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
                UpdateBuindingUpgradeAsync(i).Forget();
            return;
        }

        Release_CTS(_objectType);
        var castleData = DataManager.castle.GetCaslteData(_objectType);

        var dtUpgradeEnd = castleData.dtUpgradeEnd;

        var cts = new CancellationTokenSource();
        var token = cts.Token;
        m_cts.Add(_objectType, cts);

        // 일단 업그레이드를 시작 안했으면 리턴하자
        if (castleData.isDoingUpgrade == false)
            return;

        // 업그레이드가 가능한지 여부 확인하자.
        if (castleData.isValidUpgrade == false)
        {
            // 진행중이었다면 남은 시간을 기억해주자
            if (castleData.remainUpgradeSeconds == 0)
            {
                castleData.remainUpgradeSeconds = (float)(dtUpgradeEnd - Utils.GetUTC()).TotalSeconds;
                DataManager.castle.UpdateCastleData(castleData);
            }

            Signal.instance.StopCaslteBuildingUpgrade.Emit(castleData);
            return;
        }
        // 정지된 상태였다면 남은시간을 없애주자
        else if (castleData.remainUpgradeSeconds > 0)
        {
            castleData.tickUpgradeEnd = Utils.GetUTC().AddSeconds(castleData.remainUpgradeSeconds).Ticks;

            dtUpgradeEnd = castleData.dtUpgradeEnd;
        }

        Signal.instance.StartCaslteBuildingUpgrade.Emit(castleData);

        castleData.remainUpgradeSeconds = 0;
        DataManager.castle.UpdateCastleData(castleData);

        CastleBuildingUpgradeData updataData = new();
        updataData.objectType = castleData.type;

        int prevSecond = -1;

        while (true)
        {
            var ts = dtUpgradeEnd - Utils.GetUTC();
            if (ts.TotalSeconds <= 0)
                break;

            updataData.ts = ts;
            if (ts.Minutes > 0)
            {
                if (prevSecond != ts.Seconds)
                {
                    prevSecond = ts.Seconds;
                    Signal.instance.UpdateCaslteBuildingUpgrade.Emit(updataData);
                }
            }
            else
                Signal.instance.UpdateCaslteBuildingUpgrade.Emit(updataData);

            await UniTask.WaitForEndOfFrame(cancellationToken: token);
        }

        PopupManager.instance.AlertShow($"건물 업그레이드 완료: [{TableManager.stringTable.GetString($"CASTLE_OBJECT_{_objectType.ToString().ToUpper()}")}]");

        castleData = DataManager.castle.CompleteUpgrade(_objectType);
        Signal.instance.CompleteCaslteBuildingUpgrade.Emit(castleData);

        DataManager.castle.OnUpdateClaim();
    }

    public void Release_CTS(CastleObjectType _objectType)
    {
        if (_objectType == CastleObjectType.NONE)
        {
            for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
                Release_CTS(i);

            return;
        }

        if (m_cts.ContainsKey(_objectType))
        {
            m_cts[_objectType].Cancel();
            m_cts[_objectType].Dispose();
            m_cts.Remove(_objectType);
        }
    }

    public void Release()
    {
        Release_CTS(CastleObjectType.NONE);
    }

    public struct CastleBuildingUpgradeData
    {
        public CastleObjectType objectType;
        public TimeSpan ts;
    }
}
