using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupDailyDungeonResultComponent : BasePopupComponent
{
    PopupDailyDungeonResultComponent() : base(PopupType.DailyDungeonResult) { }

    Data_DailyDungeon.DailyDungeonRecordData m_resultData;
    public StatusType result { get; private set; } = StatusType.Wait;

    protected override void Awake()
    {
        m_element.btnConfirm.onClick.AddListener(Close);
        m_element.btnRetry.onClick.AddListener(() => RetryAsync().Forget());

        m_element.txtTitle.text = "_결과_";
    }

    private void Start()
    {
        Utils.WaitEscape(this, Close);
    }

    public override void OpenPopup(params object[] _args)
    {
        m_resultData = (Data_DailyDungeon.DailyDungeonRecordData)_args[0];

        m_element.txtResult.text = m_resultData.isSweep ? "토벌_성공" : "처치_성공";
        m_element.txtPercent.text = $"최종_결과: [{TableManager.stringTable.GetGradeType(m_resultData.gradeType, _isColor: true)}]";
        if (m_resultData.isSweep == false)
            m_element.txtPercent.text += $" ({(m_resultData.percent * 100):0.00}%)";

        SetReward();
        SetCountText();

        m_element.txtCount.gameObject.SetActive(m_resultData.isSweep == false);
        m_element.panel.ForceRebuildLayout();
    }

    async UniTask RetryAsync()
    {
        if (DataManager.dailyDungeon.data.count > 0)
        {
            result = StatusType.Success;
            Close();
        }
        else if (await DataManager.dailyDungeon.ShowAdsAsync() == true)
            SetCountText();
    }

    List<TableItemData> m_rewards;
    void SetReward()
    {
        m_rewards = TableManager.dailyDungeonGrade.GetReward(
            TableManager.dailyDungeonBoss.Get(m_resultData.weekday).dungeon_boss_class
            , m_resultData.gradeType
            , m_resultData.percent);

        int i = 0;

        var parent = m_element.pReward;
        var baseItem = parent.GetChild(0);
        for (; i < m_rewards.Count; i++)
        {
            var slot = (i == m_element.pReward.childCount ? Instantiate(baseItem, parent) : parent.GetChild(i)).GetComponent<ItemComponent>();
            slot.SetItemData(m_rewards[i]);

            m_element.pReward.gameObject.SetActive(true);
        }

        for (; i < m_element.pReward.childCount; i++)
            m_element.pReward.GetChild(i).gameObject.SetActive(false);
    }

    void SetCountText()
    {
        var data = DataManager.dailyDungeon.data;

        m_element.txtCount.text = $"일일_입장_가능_횟수: {data.count}";

        if (m_resultData.isSweep == false)
        {
            if (data.count > 0)
            {
                m_element.btnRetry.text = m_resultData.isSweep ? "토벌_하기" : "다시_하기";
                m_element.txtCountAD.text = "";
            }
            else
            {
                m_element.btnRetry.text = "광고_보기";
                m_element.txtCountAD.text = $"({data.adCount}/3)";
            }
        }

        bool isActive = data.count + data.adCount > 0 && m_resultData.isSweep == false;
        if (isActive != m_element.btnRetry.gameObject.activeSelf)
        {
            m_element.btnRetry.gameObject.SetActive(isActive);
            m_element.btnRetry.transform.parent.ForceRebuildLayout();
        }
    }

    public override void Close()
        => CloseAsync().Forget();

    async UniTask CloseAsync()
    {
        if (m_resultData.isSweep == true)
            await RewardWorker.instance.RunAsync(CameraManager.posPointer, m_rewards.ToArray());

        await Utils.SetActivePunchAsync(m_element.panel, false);
        base.Close();
    }

    #region VALIDATE
    public override void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtResult;
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtPercent;
        public Transform pReward;
        public TextMeshProUGUI txtCount;

        public ButtonHelper btnConfirm;
        public ButtonHelper btnRetry;

        public TextMeshProUGUI txtCountAD;

        public Transform panel => txtTitle.transform.parent;

        public void Initialize(Transform _transform)
        {
            txtResult = _transform.GetComponent<TextMeshProUGUI>("Panel/Result/Text");
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
            txtPercent = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_percent");
            pReward = _transform.Find("Panel/Reward");
            txtCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_count");

            btnConfirm = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_confirm");
            btnRetry = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_retry");
            txtCountAD = btnRetry.transform.GetComponent<TextMeshProUGUI>("txt_adCount");
        }
    }
    #endregion VALIDATE
}
