using Cysharp.Threading.Tasks;
using DG.Tweening;
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

        private void OnEnable()
        {
            ActionRotation();
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
            var levelReward = rewardData.level;
            bool isAvail = DataManager.pass.level >= levelReward;

            bool isReceive = isAvail && DataManager.pass.IsReceiveReward(levelReward, false);
            m_element.reward.SetActiveBadge(isReceive);

            bool isReceivePaid = isAvail && DataManager.pass.IsReceiveReward(levelReward, true);
            m_element.rewardPaid.SetActiveBadge(isReceivePaid);

            m_element.effectPaid.SetActive(isAvail && isReceivePaid == false);
            m_element.lockPaid.SetActive(DataManager.pass.isPaid == false);
            ActionRotation();
        }

        void ActionRotation()
        {
            if (rewardData == null)
                return;

            var levelReward = rewardData.level;

            m_element.reward.transform.DOKill();
            m_element.reward.transform.rotation = Quaternion.identity;
            m_element.rewardPaid.transform.DOKill();
            m_element.rewardPaid.transform.rotation = Quaternion.identity;

            UnityAction<Transform> action = _slot =>
            {
                float duration = .5f;
                _slot.DORotate(new Vector3(0, 0, -1), duration * 0.5f).SetEase(Ease.OutSine).OnComplete(() =>
                {
                    //_slot.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-1f, 1f));
                    _slot.DORotate(new Vector3(0, 0, 1), duration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).ToUniTask(cancellationToken: destroyCancellationToken);
                }).ToUniTask(cancellationToken: destroyCancellationToken);
            };

            if (DataManager.pass.level >= levelReward)
            {
                if (DataManager.pass.IsReceiveReward(levelReward, false) == false)
                    action(m_element.reward.transform);
                if (DataManager.pass.IsReceiveReward(levelReward, true) == false)
                    action(m_element.rewardPaid.transform);
            }
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

            public GameObject effectPaid;
            public GameObject lockPaid;

            public void Initialize(Transform _transform)
            {
                reward = _transform.GetComponent<ItemComponent>("Reward");
                rewardPaid = _transform.GetComponent<ItemComponent>("Reward_Paid");
                imgGauge = _transform.GetComponent<Image>("Gauge");
                txtLevel = _transform.GetComponent<TextMeshProUGUI>("Level/Text");

                effectPaid = _transform.Find("Effect").gameObject;
                lockPaid = rewardPaid.transform.Find("Lock").gameObject;
            }
        }
        #endregion VALIDATE
    }
}