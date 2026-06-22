using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using static Data_BossRaid;

public partial class InfoStage_Boss
{
    RectTransform m_rtTimer;
    CancellationTokenSource m_ctsBossRaid;

    void Awake_BossRaid()
    {
        m_rtTimer = transform.GetComponent<RectTransform>("Timer");

        Signal.instance.BossRaidStatus.connect = SlotBossRaidStatus;
    }

    void SlotBossRaidStatus(BossRaidStatusType _status)
    {
        if (_status == BossRaidStatusType.Finish_FirstPhase)
        {
            m_ctsBossRaid = m_ctsBossRaid.Release();
            m_rtTimer.gameObject.SetActive(false);
        }
        else if (_status == BossRaidStatusType.Wait_SecondPhase)
        {
            InfoStageComponent.instance.SetActive(true, true);
            m_element.rtBar.DOAnchorPosX(0, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
                Utils.AfterSecond(() => BossRaidWorker.instance.Start_SecondPhase(), .3f));
        }
        else if (_status == BossRaidStatusType.SecondPhase)
        {
            m_rtTimer.SetAnchoredPositionX(0);
            m_rtTimer.gameObject.SetActive(true);
        }
    }

    void StartBossRaid()
    {
        var dataRaid = DataManager.bossRaid.data;

        //"여포<color=#6D6D6D><size=80%> 최강무장</size></color>";
        m_element.txtName.text = $"[<color=#{Palette.GetHexa_GradeText(dataRaid.nowGrade)}>{TableManager.stringTable.GetGradeType(dataRaid.nowGrade)}</color>] {TableManager.hero.Get(dataRaid.keyBoss).name}";

        TimerAsync((dataRaid.dtEndRound - dataRaid.dtNextRound).TotalMinutes).Forget();
    }

    async UniTask TimerAsync(double _ramainTime)
    {
        m_ctsBossRaid = m_ctsBossRaid.Release(true);
        var token = m_ctsBossRaid.Token;

        var dataRaid = DataManager.bossRaid.data;

        m_rtTimer.gameObject.SetActive(true);

        var width = -((RectTransform)transform).rect.width;
        var dtEnd = dataRaid.dtEndRound.AddSeconds(-Configure.instance.timeGapFromServer);
        while (true)
        {
            var process = 1 - (dtEnd - DateTime.UtcNow).TotalMinutes / _ramainTime;

            var pos = m_rtTimer.anchoredPosition;
            pos.x = width * (float)process;
            m_rtTimer.anchoredPosition = pos;

            if (process >= 1)
                break;

            await UniTask.WaitForEndOfFrame(cancellationToken: token);
        }

        m_ctsBossRaid = m_ctsBossRaid.Release();
        m_rtTimer.gameObject.SetActive(false);
    }
}
