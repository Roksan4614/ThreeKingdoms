using Cysharp.Threading.Tasks;
using DG.Tweening;
using Rev9.Tournament;
using System;
using System.Drawing;
using System.Threading;
using TMPro;
using UnityEngine;

public class PopupTournamentResultComponent : BasePopupComponent
{
    PopupTournamentResultComponent() : base(PopupType.TournamentResult) { }

    private void Start()
    {
        m_element.btnExit.onClick.AddListener(() => OnButtonAsync_Exit().Forget());

        StartAsync().Forget();
    }

    async UniTask StartAsync()
    {
        bool isWin = TournamentHeroInfoManager.instance.IsWin();

        m_element.title.gameObject.SetActive(false);
        m_element.panel.gameObject.SetActive(false);
        m_element.btnExit.gameObject.SetActive(false);

        await UniTask.WaitForSeconds(.5f);

        m_element.txtTitle.text = isWin ? "_승리_" : "_패배_";
        m_element.title.gameObject.SetActive(true);
        await UniTask.WaitForSeconds(.5f);

        long prevPoint = TournamentWorker.instance.rankData.point;
        int rewardPoint = TournamentWorker.instance.GetResultPoint(isWin);
        int resultRank = 13543;
        int prevRank = (isWin ? -1 : 1) * UnityEngine.Random.Range(3, 10) + resultRank;

        m_element.tierPoint.text = prevPoint.AmountKMBT(_isMBT: true);
        m_element.txtRank.text = "";
        m_element.txtResult.text = "";
        m_element.reward.SetCountText(1);
        m_element.txtTimer.text = "";

        await Utils.SetActivePunchAsync(m_element.panel, true);

        m_element.txtResult.color = isWin ? Palette.color_Up : Palette.color_Down;

        var targetPoint = prevPoint + rewardPoint;
        DOTween.To(() => prevPoint,
            _result =>
            {
                var point = _result - prevPoint;
                m_element.txtResult.text = $"({(point > 0 ? "+" : "")}{point})";
                m_element.tierPoint.text = _result.AmountKMBT(_isMBT: true);
            },
            targetPoint,
            0.2f).Forget();

        var targetRank = resultRank - prevRank;
        string msgRankDesc = targetRank == 0 ? "-" : $"{(targetRank > 0 ? "+" : "")}{targetRank}";
        m_element.txtRank.text = $"{resultRank:#,0}_위 <color=#{(isWin ? Palette.htmlString_Up : Palette.htmlString_Down)}><size=90%>({msgRankDesc})";
        //m_element.txtRank.transform.DOPunchScale(Vector3.one * .1f, .1f).Forget();

        m_element.reward.SetCountText(TournamentWorker.instance.GetResultRewardCount(isWin));
        await m_element.reward.transform.DOPunchScale(Vector3.one * .1f, .1f);

        //승급 업인지 체크할거야.
        if (m_isTierUp == true)
        {

        }

        TimerAsync().Forget();
    }

    bool m_isTierUp;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            m_isTierUp = false;
            m_cts = m_cts.ReleaseCTS();
            StartAsync().Forget();
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            m_isTierUp = true;
            m_cts = m_cts.ReleaseCTS();
            StartAsync().Forget();
        }
    }

    CancellationTokenSource m_cts;
    async UniTask TimerAsync()
    {
        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;

        var dtEnd = DateTime.Now.AddSeconds(335);
        //var dtEnd = DateTime.Now.AddSeconds(5);

        m_element.btnExit.gameObject.SetActive(true);
        int prev = 0;
        while (dtEnd > DateTime.Now)
        {
            int sec = (int)(dtEnd - DateTime.Now).TotalSeconds;

            if (prev != sec)
            {
                prev = sec;
                m_element.txtTimer.text = $"{sec}초_후_자동으로_나가집니다.";
            }

            await UniTask.NextFrame(token);
        }

        OnButtonAsync_Exit().Forget();
    }

    public async UniTask OnButtonAsync_Exit()
    {
        m_cts = m_cts.ReleaseCTS();
        m_element.txtTimer.text = "";

        await PopupManager.instance.ShowDimmAsync(true);

        TournamentWorker.instance.ExitAsync().Forget();
        Close();
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;

        public UITierPointHelper tierPoint;
        public TextMeshProUGUI txtResult;
        public TextMeshProUGUI txtRank;

        public ItemComponent reward;

        public TextMeshProUGUI txtTimer;
        public ButtonHelper btnExit;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Title/Text");

            tierPoint = _transform.GetComponent<UITierPointHelper>("Panel/TierPoint");
            txtResult = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_result");
            txtRank = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_rank");

            reward = _transform.GetComponent<ItemComponent>("Panel/Reward/Slot");

            txtTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_timer");
            btnExit = _transform.GetComponent<ButtonHelper>("Panel/btn_confirm");
        }

        public Transform title => txtTitle.transform.parent;
        public Transform panel => tierPoint.transform.parent;
    }
    #endregion VALIDATE

}
