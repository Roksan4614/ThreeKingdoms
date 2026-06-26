using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using static Data_BossRaid;

public partial class InfoStage_Boss
{
    [SerializeField]
    ElementData_BossRaid m_elementBossRiad;
    CancellationTokenSource m_ctsBossRaid;

    RectTransform rtTimer => m_elementBossRiad.rtTimer;

    [System.Serializable]
    struct ElementData_BossRaid
    {
        public TextMeshProUGUI txtTimer;
        public RectTransform rtTimer;

        public RectTransform rtUsers;
        public float posUserX_MAX;

        public void Initialize(Transform _transform)
        {
            txtTimer = _transform.GetComponent<TextMeshProUGUI>("Timer/txt_timer");

            if (txtTimer == null)
                return;

            rtTimer = (RectTransform)txtTimer.transform.parent;

            rtUsers = (RectTransform)_transform.Find("FX_Users");
            posUserX_MAX = rtUsers.anchoredPosition.x;
        }
    }

    void Awake_BossRaid()
    {
        Signal.instance.BossRaidStatus.connect = SlotBossRaidStatus;
    }

    void SlotBossRaidStatus(BossRaidStatusType _status)
    {
        if (_status == BossRaidStatusType.Finish_FirstPhase)
        {
            m_ctsBossRaid = m_ctsBossRaid.Release();
            rtTimer.gameObject.SetActive(false);
        }
        else if (_status == BossRaidStatusType.Wait_SecondPhase)
        {
            InfoStageComponent.instance.SetActive(true, true);
            StartBossRaid(false);

            m_elementBossRiad.rtUsers.SetAnchoredPositionX(m_elementBossRiad.posUserX_MAX);

            m_element.rtBar.DOAnchorPosX(0, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
                Utils.AfterSecond(() => BossRaidWorker.instance.Start_SecondPhase(), .3f));
        }
        else if (_status == BossRaidStatusType.SecondPhase)
        {
            rtTimer.SetAnchoredPositionX(0);
            rtTimer.gameObject.SetActive(true);
            m_elementBossRiad.txtTimer.text = "";

            var dataRaid = DataManager.bossRaid.data;
            TimerAsync((dataRaid.dtEndRound - dataRaid.dtSecondPhase).TotalMinutes).Forget();
        }
    }

    void StartBossRaid(bool _isStartTimer)
    {
        var dataRaid = DataManager.bossRaid.data;

        m_element.txtName.text = dataRaid.bossName;

        if (_isStartTimer)
            TimerAsync((dataRaid.dtEndRound - dataRaid.dtNextRound).TotalMinutes).Forget();
    }

    async UniTask TimerAsync(double _ramainTime)
    {
        m_ctsBossRaid = m_ctsBossRaid.Release(true);
        var token = m_ctsBossRaid.Token;

        var dataRaid = DataManager.bossRaid.data;

        rtTimer.gameObject.SetActive(true);

        var width = -((RectTransform)transform).rect.width;
        var dtEnd = dataRaid.dtEndRound.AddSeconds(-Configure.instance.timeGapFromServer);
        while (true)
        {
            var ts = (dtEnd - DateTime.UtcNow);
            var process = 1 - ts.TotalMinutes / _ramainTime;

            var pos = rtTimer.anchoredPosition;
            pos.x = width * (float)process;
            rtTimer.anchoredPosition = pos;

            m_elementBossRiad.txtTimer.text = ts.ToRemainTime(15, _isStartMinute: true);

            if (process >= 1)
                break;

            await UniTask.WaitForEndOfFrame(cancellationToken: token);
        }

        m_ctsBossRaid = m_ctsBossRaid.Release();
        rtTimer.gameObject.SetActive(false);
    }

    Tween m_tweenMoveUsers;
    void SlotUpdateBossHP_BossRaid(float _target)
    {
        m_tweenMoveUsers?.Kill();

        if (_target < m_elementBossRiad.rtUsers.anchoredPosition.x)
            m_tweenMoveUsers = m_elementBossRiad.rtUsers.DOAnchorPosX(_target, 0.5f);
    }
}
