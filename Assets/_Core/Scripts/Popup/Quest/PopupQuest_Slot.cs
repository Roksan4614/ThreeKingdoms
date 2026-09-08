using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupQuest_Slot : MonoBehaviour, IValidatable
{
    public System.Action<PopupQuest_Slot, QuestInfoData> actionConfirm { get; set; }

    private void Start()
    {
        m_element.btnConfirm.onClick.AddListener(() => actionConfirm(this, questData));
    }

    public QuestInfoData questData { get; private set; }
    public void SetQuestData(QuestInfoData _questData)
    {
        questData = _questData;

        m_element.txtTitle.text = questData.name;

        var reward = questData.data.reward;
        m_element.reward.SetItemData(reward);
        m_element.reward.SetCountText(0);
        m_element.txtReward.text = $"{reward.name}{(reward.count == 0 ? "" : $" x{reward.count}")}";

        UpdateStatus();
    }

    public void UpdateStatus()
    {
        m_element.txtCount.text = questData.isComplete ? "" : $"({questData.count}/{questData.data.target_value})";

        m_element.completeBadge.SetActive(questData.isReceiveReward);

        m_element.btnConfirm.gameObject.SetActive(questData.isReceiveReward == false);
        m_element.btnConfirm.text = TableManager.stringTable.GetString($"BUTTON_{(questData.isComplete ?"RECEIVE":"NAVIGATION")}");

        if (m_element.btnConfirm.isDrawSelect != questData.isComplete && questData.isReceiveReward == false)
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
        public GameObject completeBadge;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Title/Text");
            txtCount = _transform.GetComponent<TextMeshProUGUI>("Title/txt_count");
            reward = _transform.GetComponent<ItemComponent>("Reward");

            txtReward = _transform.GetComponent<TextMeshProUGUI>("txt_reward");
            btnConfirm = _transform.GetComponent<ButtonHelper>("btn_confirm");

            completeBadge = _transform.Find("Badge").gameObject;
        }
    }
    #endregion VALIDATE

}
