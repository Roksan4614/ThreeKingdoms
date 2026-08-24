using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ContentMarketTabType
{
    NONE = -1,

    Daily, Tournament, Raid,

    MAX
}

public class PopupContentMarketComponent : BasePopupComponent
{
    PopupContentMarketComponent() : base(PopupType.ContentMarket) { }

    ContentMarketTabType m_curTab;

    Dictionary<ContentMarketTabType, TabData> m_dbTab = new();

    private void Start()
    {
        transform.GetComponent<Image>().enabled = true;
        m_element.guide.talkbox.SetMaxWidth(640);

        // SetTab
        {
            var content = m_element.scrollTab.content;
            var dot = m_element.scrollTab.transform.Find("Dot");
            for (var i = ContentMarketTabType.NONE + 1; i < ContentMarketTabType.MAX; i++)
            {
                int idx = (int)i;
                var slot = idx < content.childCount ? content.GetChild(idx) : Instantiate(content.GetChild(0), content);

                TabData data = new();
                data.button = slot.GetComponent<ButtonHelper>();
                data.button.text = TableManager.stringTable.GetString("CONTENT_MARKET_TAB_" + i.ToString().ToUpper());
                data.imgDot = (idx < dot.childCount ? dot.GetChild(idx) : Instantiate(dot.GetChild(0), dot)).GetComponent<Image>();

                m_dbTab.Add(i, data);
            }
            content.parent.ForceRebuildLayout();
        }

        foreach (var tab in m_dbTab)
            tab.Value.button.onClick.AddListener(() => OnButton_Tab(tab.Key));

        for (int i = 0; i < m_element.panelPopup.childCount; i++)
            m_element.panelPopup.GetChild(i).gameObject.SetActive(false);

        OnButton_Tab(m_curTab, true);

        Utils.WaitEscape(this, () =>
        {
            if (m_element.popupBuy.CloseEscape() == false)
                Close();
        });
    }

    public override void OpenPopup(params object[] _args)
    {
        m_curTab = _args.Length == 0 ? ContentMarketTabType.NONE : (ContentMarketTabType)_args[0];

        Utils.SetActivePunch(m_element.panel, true);
    }

    public override void Close()
    {
        Utils.SetActivePunch(m_element.panel, false, _callback: base.Close);
    }

    private void OnDestroy()
    {
        m_ctsTimer = m_ctsTimer.ReleaseCTS();
        m_ctsTalk = m_ctsTalk.ReleaseCTS();
    }

    void OnButton_Tab(ContentMarketTabType _tabType, bool _isForce = false)
    {
        if (m_curTab == _tabType && _isForce == false)
            return;

        TabData curTabData = new();
        if (_isForce)
        {
            foreach (var tab in m_dbTab)
            {
                bool isCurrent = tab.Key == _tabType;
                tab.Value.button.SetDrawSelect(isCurrent);
                tab.Value.imgDot.color = isCurrent ? Color.black : Color.gray8;

                if (isCurrent == true)
                    curTabData = tab.Value;
            }
        }
        else
        {
            m_dbTab[m_curTab].button.SetDrawSelect(false);
            m_dbTab[m_curTab].imgDot.color = Color.gray8;

            curTabData = m_dbTab[_tabType];
            curTabData.button.SetDrawSelect(true);
            curTabData.imgDot.color = Color.black;
        }

        //닷 펀치해주자
        curTabData.imgDot.transform.DOPunchScale(Vector3.one * .1f, .1f);

        m_curTab = _tabType;

        GuildeTextAsync().Forget();
        SetProductLayout();

        TimerAsync().Forget();
    }

    CancellationTokenSource m_ctsTalk;
    async UniTask GuildeTextAsync()
    {
        m_ctsTalk = m_ctsTalk.ReleaseCTS(true);
        var token = m_ctsTalk.Token;

        var message = TableManager.stringTable.GetString($"CONTENT_MARKET_TAB_{m_curTab.ToString().ToUpper()}_DESC");
        var curTabData = m_dbTab[m_curTab];
        if (curTabData.message.IsActive() == false)
        {
            curTabData.message = message;
            m_dbTab[m_curTab] = curTabData;

            m_element.guide.talkbox.SetTyping(false);
            m_element.guide.anim.Play("Talk", 1);
            m_element.guide.talkbox.Start(token, message);

            await UniTask.WaitUntil(() => m_element.guide.talkbox.isTyping == true, cancellationToken: token);
            m_element.guide.talkbox.rt.pivot = new Vector2(1, .7f);
            await UniTask.WaitUntil(() => m_element.guide.talkbox.isTyping == false, cancellationToken: token);

            m_element.guide.anim.Play("NONE", 1);
        }
        else
        {
            m_element.guide.talkbox.Init(message);
            m_element.guide.talkbox.rt.pivot = new Vector2(1, .7f);
            m_element.guide.anim.Play("Talk", 1);
            await UniTask.WaitForSeconds(1f, cancellationToken: token);
            m_element.guide.anim.Play("NONE", 1);
        }
    }

    CancellationTokenSource m_ctsTimer;
    async UniTask TimerAsync()
    {
        m_ctsTimer = m_ctsTimer.ReleaseCTS(true);
        var token = m_ctsTimer.Token;

        var utc = Utils.GetUTC();
        DateTime endTime;

        int addHours = 0;

        if (m_curTab == ContentMarketTabType.Daily)
            endTime = utc.Date.AddDays(1).AddHours(addHours);
        else if (m_curTab == ContentMarketTabType.Tournament)
            endTime = Utils.GetNextMidnight(DayOfWeek.Monday);
        else
            endTime = Utils.GetNextMonthMidnight(1);

        TimeSpan ts = endTime - Utils.GetUTC();
        int prevSec = -1;
        while (ts.TotalSeconds > 0)
        {
            if (ts.TotalSeconds <= 10f || prevSec != ts.Seconds)
            {
                prevSec = ts.Seconds;
                m_element.txtTimer.text = $"남은시간_: <color=#000000>{ts.ToRemainTime(28)}";
            }

            await UniTask.NextFrame(token);
            ts = endTime - Utils.GetUTC();
        }

        m_element.txtTimer.text = "_정산중_";
    }

    void SetProductLayout()
    {
        var dbItem = TableManager.item.list;

        var content = m_element.scrollProduct.content;
        int i = 0;
        for (; i < dbItem.Count; i++)
        {
            var slot = (i < content.childCount ? content.GetChild(i) : Instantiate(content.GetChild(0), content))
                .GetComponent<PopupContentMarket_Slot>();
            slot.SetProductData(dbItem[i], OnButton_Product);
        }

        for (; i < content.childCount; i++)
            content.GetChild(i).gameObject.SetActive(false);

        content.ForceRebuildLayout();
        m_element.scrollProduct.velocity = content.anchoredPosition = Vector2.zero;
    }

    void OnButton_Product(TableItemData _itemData)
    {
        m_element.popupBuy.SetProductData(_itemData);
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ScrollRect scrollTab;

        public ScrollRect scrollProduct;
        public CharacterComponent guide;
        public TextMeshProUGUI txtTimer;

        public PopupContentMarket_Popup_Buy popupBuy;

        public void Initialize(Transform _transform)
        {
            scrollTab = _transform.GetComponent<ScrollRect>("Panel/Tab");

            scrollProduct = _transform.GetComponent<ScrollRect>("Panel/Market/Scroll");
            guide = scrollProduct.transform.GetComponent<CharacterComponent>("Host/Guide");
            txtTimer = scrollProduct.transform.GetComponent<TextMeshProUGUI>("txt_timer");
            popupBuy = _transform.GetComponent<PopupContentMarket_Popup_Buy>("Popup/Buy");
        }

        public Transform panel => scrollTab.transform.parent;
        public Transform panelPopup => popupBuy.transform.parent;
    }
    #endregion VALIDATE

    struct TabData
    {
        public ButtonHelper button;
        public Image imgDot;
        public string message;
    }

}
