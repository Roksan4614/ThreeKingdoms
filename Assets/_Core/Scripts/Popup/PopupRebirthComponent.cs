using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class PopupRebirthComponent : BasePopupComponent
{
    PopupRebirthComponent() : base(PopupType.Rebirth) { }

    bool m_isLockClose;

    private void Start()
    {
        m_element.btnCancel.onClick.AddListener(Close);
        m_element.btnConfirm.onClick.AddListener(() => OnButtonAsync_Confirm().Forget());

        Utils.WaitEscape(this, Close, _isMenuPopup: true);

        Signal.instance.StartStage.connectLambda = new(this, _ =>
        {
            if (gameObject.activeInHierarchy == true)
                SetRebirthInfo();
        });

        //setlocalization
        {
            m_element.txtTitle.text = TableManager.stringTable.GetString("UI_REBIRTH_TITLE");
            m_element.txtStageTitle_Prev.text = TableManager.stringTable.GetString("UI_DIFFICULT_NOW");
            m_element.txtStageTitle_Next.text = TableManager.stringTable.GetString("UI_DIFFICULT_AFTER_REBIRTH");
            m_element.txtRewardTitle.text = TableManager.stringTable.GetString("UI_RECEIVE_AMOUNT_TIME_STONE");
            m_element.btnConfirm.text = TableManager.stringTable.GetString("BUTTON_CONFIRM");
            m_element.btnCancel.text = TableManager.stringTable.GetString("BUTTON_CANCEL");
        }
    }

    public override void OpenPopup(params object[] _args)
    {
        m_isLockClose = false;
        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);

        SetRebirthInfo();
    }

    void SetRebirthInfo()
    {
        var stageData = StageManager.instance.data;
        m_element.txtStage_Prev.text = stageData.stageFullName;

        StageManager.LoadData_Stage nextStageData = new()
        {
            chapterNumber = 1,
            stageNumber = 1,
            level = Mathf.Max(1, stageData.level - 2)
        };
        m_element.txtStage_Next.text = nextStageData.stageFullName;

        var rebirthData = StageManager.instance.rebirthData;
        StageManager.LoadData_Stage minStageData = new()
        {
            chapterNumber = 1,
            stageNumber = 1,
            level = rebirthData == null ? 2 : rebirthData.level + 1
        };

        bool isAvail = stageData.level >= minStageData.level;
        m_element.btnConfirm.interactable = isAvail;
        m_element.txtDesc.text = isAvail ? ""
            : TableManager.stringTable.GetStringFormat("UI_REBIRTH_INFO", minStageData.stageFullName);// $"{}_도달_후_가능";

        m_element.btnConfirm.transform.GetComponent<CanvasGroup>().alpha = isAvail ? 1 : .6f;

        var point = stageData.level * 100 + stageData.chapterNumber * 10 + stageData.stageNumber;
        m_element.txtRewardCount.text = point.ToString("#,0");
    }

    async UniTask OnButtonAsync_Confirm()
    {
        m_isLockClose = true;

        PopupManager.instance.CloseAll(popupType);

        var dimm = transform.Find("Dimm").gameObject;
        dimm.SetActive(false);

        await StageManager.instance.StartRebirthAsync();
        m_element.panel.gameObject.SetActive(false);

        await UniTask.WaitUntil(() => PopupManager.instance.isDimm == false);
        await RewardWorker.instance.RunAsync(m_element.panel.position, _itemData: TableManager.item.GetItemData("time_stone", 100));

        gameObject.SetActive(false);
        dimm.SetActive(true);
    }

    public override void Close()
    {
        if (m_isLockClose == true)
            return;

        Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtStageTitle_Prev;
        public TextMeshProUGUI txtStageTitle_Next;
        public TextMeshProUGUI txtStage_Prev;
        public TextMeshProUGUI txtStage_Next;
        public TextMeshProUGUI txtRewardTitle;
        public TextMeshProUGUI txtRewardCount;
        public TextMeshProUGUI txtDesc;
        public ButtonHelper btnCancel;
        public ButtonHelper btnConfirm;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
            txtStageTitle_Prev = _transform.GetComponent<TextMeshProUGUI>("Panel/Target/Title/txt_prev");
            txtStageTitle_Next = _transform.GetComponent<TextMeshProUGUI>("Panel/Target/Title/txt_next");
            txtStage_Prev = _transform.GetComponent<TextMeshProUGUI>("Panel/Target/txt_prev");
            txtStage_Next = _transform.GetComponent<TextMeshProUGUI>("Panel/Target/txt_next");
            txtRewardTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_reward_title");
            txtRewardCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_reward_count");
            txtDesc = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_desc");

            btnConfirm = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_confirm");
            btnCancel = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_cancel");
        }

        public Transform panel => txtRewardCount.transform.parent;
    }
    #endregion VALIDATE

}
