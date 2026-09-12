using Cysharp.Threading.Tasks;
using System;
using System.Globalization;
using ThreeKingdoms.Shared.Types;
using ThreeKingdoms.Client.Server;
using UnityEngine;

public partial class PopupBossRaidResultComponent
{
    private bool serverResultPending;
    private async UniTask OpenServerResultAsync()
    {
        if (serverResultPending) return;
        serverResultPending = true;
        Utils.SetActivePunch(transform, true);
        m_element.btnConfirm.interactable = false;
        m_element.txtName.text = DataManager.bossRaid.data.bossName;
        m_element.txtRank.text = "서버 정산 확인 중";
        m_element.txtDamage.text = "";
        m_element.txtPoint.text = "";
        m_element.imgSuccess.SetActive(BossRaidWorker.instance.isSuccessed);
        m_element.imgFail.SetActive(!BossRaidWorker.instance.isSuccessed);
        try
        {
            while (!destroyCancellationToken.IsCancellationRequested)
            {
                try
                {
                    var claim = await DataManager.bossRaid.ClaimServerAsync();
                    var rank = DataManager.bossRaid.ServerRanking;
                    m_element.txtRank.text = rank.Me == null ? "순위 없음" : $"{rank.Me.Rank}위 / {rank.ParticipantCount}명 ({rank.Me.Percentile:0.00}%)";
                    m_element.txtDamage.text = "누적 피해: " + (rank.Me?.Damage ?? "0");
                    m_element.txtPoint.text = "획득 포인트: " + claim.RewardPoints + "\n시즌 잔액: " + claim.RaidPointBalance;
                    break;
                }
                catch (GameServerException error) when (error.Code == "RAID_REWARD_NOT_READY")
                { await UniTask.Delay(1000, cancellationToken: destroyCancellationToken); }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { m_element.txtPoint.text = "정산 확인 실패: " + error.Message; GameServer.Report(error); }
        finally { serverResultPending = false; m_element.btnConfirm.interactable = true; }
    }
}

public partial class PopupLobbyBossRaidComponent
{
    private bool serverSummaryPending;
    private float nextServerSummaryRefresh;
    private string displayedServerRaidBalance;

    private void InitializeServerSummaryView()
    {
        m_element.txtPoint.text = "조회 중";
        m_element.txtInfoPrevRound.text = "최근 참가 기록 조회 중";
        displayedServerRaidBalance = null;
        RefreshServerSummaryAsync().Forget();
    }

    private static string FormatServerRaidAmount(string value)
    {
        var number = System.Numerics.BigInteger.Parse(value, CultureInfo.InvariantCulture);
        if (number.Sign < 0) throw new InvalidOperationException("RAID_SUMMARY_AMOUNT_INVALID");
        return number.ToString("N0", CultureInfo.InvariantCulture);
    }

    private void UpdateServerPointView()
    {
        var balance = DataManager.bossRaid.ServerRaid?.RaidPointBalance;
        if (balance == null || balance == displayedServerRaidBalance) return;
        m_element.txtPoint.text = FormatServerRaidAmount(balance) + "p";
        displayedServerRaidBalance = balance;
    }

    private async UniTask RefreshServerSummaryAsync()
    {
        if (serverSummaryPending || !gameObject.activeInHierarchy) return;
        serverSummaryPending = true;
        try
        {
            await DataManager.bossRaid.RefreshServerAsync();
            if (this == null || !gameObject.activeInHierarchy) return;
            UpdateServerPointView();
            var lastId = DataManager.bossRaid.ServerRaid?.LastParticipatedRaidId;
            if (string.IsNullOrEmpty(lastId))
            {
                m_element.txtInfoPrevRound.text = "최근 참가 기록이 없습니다.";
                return;
            }
            var response = await GameServer.Raid.RankAsync(new RaidRankReq { RaidId = lastId }, GameServer.Options(), destroyCancellationToken);
            if (this == null || !gameObject.activeInHierarchy || DataManager.bossRaid.ServerRaid?.LastParticipatedRaidId != lastId) return;
            var rank = response.Data ?? throw new InvalidOperationException("RAID_PREVIOUS_RANK_MISSING");
            if (rank.Me == null)
            {
                m_element.txtInfoPrevRound.text = "최근 참가 기록을 확인할 수 없습니다.";
                return;
            }
            m_element.txtInfoPrevRound.text = $"최근 참가: {rank.Me.Rank}위 / {rank.ParticipantCount}명\n피해 {FormatServerRaidAmount(rank.Me.Damage)} · {(rank.Settled ? "정산 완료" : "진행 중")}";
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            if (this != null && gameObject.activeInHierarchy)
                m_element.txtInfoPrevRound.text = "최근 참가 조회 실패: " + (error is GameServerException server ? server.Code : "응답 확인 필요");
            GameServer.Report(error);
        }
        finally
        {
            serverSummaryPending = false;
            nextServerSummaryRefresh = Time.realtimeSinceStartup + 5;
        }
    }

    private async UniTask ServerRoundTimerAsync()
    {
        try
        {
            while (!destroyCancellationToken.IsCancellationRequested)
            {
                var raid = DataManager.bossRaid;
                if (gameObject.activeInHierarchy)
                {
                    UpdateServerPointView();
                    if (!serverSummaryPending && Time.realtimeSinceStartup >= nextServerSummaryRefresh) RefreshServerSummaryAsync().Forget();
                }
                var round = raid.ServerRound;
                var active = round?.Phase == "normal" || round?.Phase == "jin";
                m_element.btnStart.interactable = active && (round.CanJoin || raid.ServerRaid?.Participant != null);
                m_element.btnStart.text = round == null ? "레이드 준비 중" : active ? "레이드 참가" : round.Phase == "transition" ? "진 레이드 전환 중" : round.Phase == "finished" ? "정산 중" : "등장 대기";
                m_element.txtRoundRemainTimer.text = round == null ? "" : TimeSpan.FromSeconds(raid.ServerRemainingSeconds).ToRemainTime(25, _isStartMinute: true);
                await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
            }
        }
        catch (OperationCanceledException) { }
    }
}

public partial class Banner_BossRaid
{
    private async UniTask ServerBannerTimerAsync()
    {
        try
        {
            while (!destroyCancellationToken.IsCancellationRequested)
            {
                var raid = DataManager.bossRaid;
                var phase = raid.ServerRound?.Phase;
                m_element.button.text = phase == "normal" ? "레이드 진행 중" : phase == "jin" ? "진 레이드 진행 중"
                    : phase == "transition" ? "진 레이드 전환 중" : phase == "finished" ? "레이드 정산 중"
                    : phase == "waiting" ? "등장까지 " + TimeSpan.FromSeconds(raid.ServerRemainingSeconds).ToRemainTime(20, _isStartMinute: true) : "레이드 준비 중";
                await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
            }
        }
        catch (OperationCanceledException) { }
    }
}
