using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupQuest_Slot : MonoBehaviour, IValidatable
{
    public System.Action<PopupQuest_Slot, QuestInfoData> actionConfirm { get; set; }
    public Transform trnsRewardIcon => m_element.reward.transform;

    private void Start()
    {
        m_element.btnConfirm.onClick.AddListener(() => actionConfirm(this, questData));

        // setlocalization
        m_element.badge.text = TableManager.stringTable.GetString("COMPLETE_REWARD");
    }

    public QuestInfoData questData { get; private set; }
    public void SetQuestData(QuestInfoData _questData)
    {
        questData = _questData;

        m_element.txtTitle.text = questData.name;

        var rewardItem = questData.data.itemData;
        m_element.reward.SetItemData(rewardItem);
        m_element.reward.SetCountText(0);
        m_element.txtReward.text = $"{rewardItem.name}{(rewardItem.count == 0 ? "" : $" x{rewardItem.count}")}";

        UpdateStatus(true);
    }

    public void UpdateStatus(bool _isInit)
    {
        m_element.txtCount.text = questData.isComplete
            ? $"({TableManager.stringTable.GetString("COMPLETE")})"
            : $"({questData.count}/{questData.data.target_value})";

        m_element.badge.transform.parent.gameObject.SetActive(questData.isReceiveReward);

        if (QuestWorker.instance.HasNavigation(questData.key) == false && questData.isComplete == false)
        {
            m_element.btnConfirm.gameObject.SetActive(false);
            return;
        }

        m_element.btnConfirm.gameObject.SetActive(questData.isReceiveReward == false);
        m_element.btnConfirm.text = TableManager.stringTable.GetString($"BUTTON_{(questData.isComplete ? "RECEIVE" : "NAVIGATION")}");

        if (_isInit == true || m_element.btnConfirm.isDrawSelect != questData.isComplete && questData.isReceiveReward == false)
            m_element.btnConfirm.SetDrawSelect(questData.isComplete);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtCount;
        public TextMeshProUGUI txtReward;
        public ItemComponent reward;
        public ButtonHelper btnConfirm;

        public BadgeHelper badge;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Title/Text");
            txtCount = _transform.GetComponent<TextMeshProUGUI>("Title/txt_count");
            reward = _transform.GetComponent<ItemComponent>("Reward");

            txtReward = _transform.GetComponent<TextMeshProUGUI>("txt_reward");
            btnConfirm = _transform.GetComponent<ButtonHelper>("btn_confirm");

            badge = _transform.GetComponent<BadgeHelper>("Badge/Badge");
        }
    }
    #endregion VALIDATE

}
