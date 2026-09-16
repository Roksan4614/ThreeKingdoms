using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rev9.Pass
{
    public class PopupPass_Reward_Slot : MonoBehaviour, IValidatable
    {
        public UnityAction<PopupPass_Reward_Slot, bool> actionReward { get; set; }

        public TablePassRewardData rewardData { get; private set; }
        public RectTransform rt => (RectTransform)transform;

        Color m_clrGreen;
        private void Awake()
        {
            m_clrGreen = m_element.imgGauge?.color ?? Color.white;

            m_element.reward.transform.GetComponent<Button>()
                .onClick.AddListener(() => actionReward(this, false));
            m_element.rewardPaid.transform.GetComponent<Button>()
                .onClick.AddListener(() => actionReward(this, true));
        }

        public void SetRewardData(TablePassRewardData _rewardData)
        {
            rewardData = _rewardData;

            m_element.txtLevel.text = _rewardData.level.ToString();

            m_element.reward.SetItemData(_rewardData.itemData);
            m_element.rewardPaid.SetItemData(_rewardData.itemDataPaid);

            RefreshBadge();
            RefreshLevelExp();
        }

        public void RefreshLevelExp()
        {
            if (m_element.imgGauge != null)
                m_element.imgGauge.color = DataManager.pass.level >= rewardData.level ? m_clrGreen : Color.white;
        }

        public void RefreshBadge()
        {
            m_element.reward.SetActiveBadge(DataManager.pass.IsReceiveReward(rewardData.level, false));

            bool isReceivePaid = DataManager.pass.IsReceiveReward(rewardData.level, true);
            m_element.rewardPaid.SetActiveBadge(isReceivePaid);

            m_element.objPaidEffect.SetActive(DataManager.pass.level >= rewardData.level && isReceivePaid == false);

        }

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public ItemComponent reward;
            public ItemComponent rewardPaid;
            public Image imgGauge;
            public TextMeshProUGUI txtLevel;

            public GameObject objPaidEffect;

            public void Initialize(Transform _transform)
            {
                reward = _transform.GetComponent<ItemComponent>("Reward");
                rewardPaid = _transform.GetComponent<ItemComponent>("Reward_Paid");
                imgGauge = _transform.GetComponent<Image>("Gauge");
                txtLevel = _transform.GetComponent<TextMeshProUGUI>("Level/Text");

                objPaidEffect = rewardPaid.transform.Find("Effect").gameObject;
            }
        }
        #endregion VALIDATE
    }
}