using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupRewardIdleComponent : BasePopupComponent
{
    PopupRewardIdleComponent() : base(PopupType.Reward_Idle) { }

    CancellationTokenSource m_cts;

    private void Start()
    {
        m_element.panel.gameObject.SetActive(false);

        m_element.btnAD.onClick.AddListener(() => OnButtonAsync_Confirm(true).Forget());
        m_element.btnConfirm.onClick.AddListener(() => OnButtonAsync_Confirm(false).Forget());
    }

    public override void OpenPopup(params object[] _args)
    {
        gameObject.SetActive(true);
        TimerAsync().Forget();
    }

    async UniTask TimerAsync()
    {
        await LoadRewardData(false);

        var idleRewardData = DataManager.userInfo.idleRewardData;

        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;

        int prevSec = -1;
        while (true)
        {
            int sec = (int)idleRewardData.tsReceive.TotalSeconds;
            if (prevSec != sec)
            {
                m_element.gauge.fillAmount = idleRewardData.percent;
                m_element.gauge.textAmount = $"{idleRewardData.tsReceive.ToRemainTime(25, _isStartMinute: true)} ({idleRewardData.percent * 100:0.#0}%)";
                prevSec = sec;
            }

            if (idleRewardData.tsRefresh.TotalMinutes > 1)
                await LoadRewardData(true);

            await UniTask.NextFrame(token);

            if (idleRewardData.percent == 1)
                await UniTask.WaitUntil(() => idleRewardData.percent < 1);
        }
    }

    async UniTask LoadRewardData(bool _isForceAPI)
    {
        await DataManager.userInfo.API_RefreshIdleReward(_isForceAPI);
        m_element.panel.gameObject.SetActive(true);

        SetRewardList(DataManager.userInfo.idleRewardData.rewards);
    }

    void SetRewardList(List<ItemData> _rewards)
    {
        var content = m_element.scroll.content;
        int i = 0;
        for (; i < _rewards.Count; i++)
        {
            var slot = (i == content.childCount ? Instantiate(content.GetChild(0), content) : content.GetChild(i))
                .GetComponent<ItemComponent>();
            slot.gameObject.SetActive(true);
            slot.SetItemData(_rewards[i]);
        }
        for (; i < content.childCount; i++)
            content.GetChild(i).gameObject.SetActive(false);

        m_element.txtEmpty.gameObject.SetActive(_rewards.Count == 0);
        content.ForceRebuildLayout();
    }

    async UniTask OnButtonAsync_Confirm(bool _isAD)
    {
        var rewards = await DataManager.userInfo.API_ReceiveIdleReward();

        if (rewards.Count == 0)
        {
            PopupManager.instance.AlertShow("받은_보상이_없습니다.");
            return;
        }

        if (_isAD)
        {
            if (await AdsManager.instance.ShowAsync() == false)
                return;

            await UniTask.WaitForSeconds(.5f);
        }

        SetRewardList(rewards);

        var content = m_element.scroll.content;

        for (int i = 0; i < rewards.Count; i++)
        {
            var reward = rewards[i];
            if (_isAD == true)
                reward.count += (int)(reward.count * 1.5f);
            RewardWorker.instance.Run(content.GetChild(i).position, _itemData: reward);
        }

        await UniTask.WaitForSeconds(.25f);

        Close();
    }

    private void OnDisable()
        => m_cts = m_cts.ReleaseCTS();

    public override void Close()
        => Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;
        public GaugeHelper gauge;
        public ScrollRect scroll;
        public TextMeshProUGUI txtEmpty;

        public ButtonHelper btnAD;
        public ButtonHelper btnConfirm;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
            gauge = _transform.GetComponent<GaugeHelper>("Panel/Gauge");
            scroll = _transform.GetComponent<ScrollRect>("Panel/Reward");
            txtEmpty = scroll.viewport.GetComponent<TextMeshProUGUI>("txt_empty");

            btnAD = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_ad");
            btnConfirm = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_confirm");
        }

        public Transform panel => txtTitle.transform.parent;
    }
    #endregion VALIDATE

}
