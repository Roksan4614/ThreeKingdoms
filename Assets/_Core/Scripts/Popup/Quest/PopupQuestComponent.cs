using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Quest
{
    public class PopupQuestComponent : BasePopupComponent
    {
        PopupQuestComponent() : base(PopupType.Quest) { }

        QuestCategoryType m_curTab;

        Dictionary<QuestCategoryType, ButtonHelper> m_tabs = new();
        List<PopupQuest_Slot> m_slots = new();
        List<Button> m_btnReward = new();

        private void Start()
        {
            for (var i = QuestCategoryType.NONE + 1; i < QuestCategoryType.MAX; i++)
            {
                int idx = (int)i;

                var tab = (idx == m_element.pTabs.childCount ?
                    Instantiate(m_element.pTabs.GetChild(0), m_element.pTabs) :
                    m_element.pTabs.GetChild(idx)).GetComponent<ButtonHelper>();

                m_tabs.Add(i, tab);
            }

            foreach (var tab in m_tabs)
            {
                tab.Value.text = TableManager.questString.GetString("CATEGORY_NAME_" + tab.Key.ToString().ToUpper());
                tab.Value.onClick.AddListener(() => SetTab(tab.Key));
            }

            for (int i = 0; i < m_element.scroll.content.childCount; i++)
                m_element.scroll.content.GetChild(i).gameObject.SetActive(false);

            Signal.instance.Quest_UpdateStatus.connect = SlotUpdateStatus;
            Utils.WaitEscape(this, Close, _isMenuPopup: true);
        }

        public override void OpenPopup(params object[] _args)
        {
            gameObject.SetActive(true);
            Utils.SetActivePunch(m_element.panel, true);

            m_curTab = QuestCategoryType.NONE;
            SetTab(QuestCategoryType.daily);
        }

        void SetTab(QuestCategoryType _category)
        {
            if (m_curTab == _category)
                return;

            if (m_curTab > QuestCategoryType.NONE)
                m_tabs[m_curTab].SetDrawSelect(false);

            m_curTab = _category;
            m_tabs[m_curTab].SetDrawSelect(true);

            SetQuestList();
            SetGaugeRewardList();

            m_element.scroll.velocity =
            m_element.scroll.content.anchoredPosition = Vector2.zero;
        }

        void SetQuestList()
        {
            var db = TableManager.quest.GetQuestList(m_curTab);

            int i = 0;
            var content = m_element.scroll.content;
            for (; i < db.Count; i++)
            {
                bool isNew = i == content.childCount;

                var slot = (isNew ? Instantiate(content.GetChild(0), content) : content.GetChild(i)).GetComponent<PopupQuest_Slot>();
                slot.gameObject.SetActive(true);
                slot.SetQuestData(QuestWorker.instance.GetQuestData(m_curTab, db[i].key));

                if (slot.actionConfirm == null)
                {
                    slot.actionConfirm = (_slot, _questData) => OnButtonAsync_Confirm(_slot, _questData).Forget();
                    m_slots.Add(slot);
                }
            }

            foreach (var slot in m_slots)
            {
                if (slot.questData.isReceiveReward)
                    slot.transform.SetAsLastSibling();
            }

            for (; i < content.childCount; i++)
                content.GetChild(i).gameObject.SetActive(false);

            content.ForceRebuildLayout();
        }

        async UniTask OnButtonAsync_Confirm(PopupQuest_Slot _slot, QuestInfoData _questData)
        {
            if (_questData.isComplete == true)
            {
                if (await QuestWorker.instance.API_ReceiveReward(_questData) == true)
                {
                    _slot.UpdateStatus();
                    _slot.transform.SetAsLastSibling();
                }
            }
        }

        void SlotUpdateStatus(QuestInfoData _questData)
        {
            if (gameObject.activeSelf == false || _questData.type != m_curTab)
                return;

            var slot = m_slots.Find(x => x.questData == _questData);
            slot.UpdateStatus();
        }

        void SetGaugeRewardList()
        {
            var dbRewards = TableManager.questReward.GetRewards(m_curTab);

            int countComplete = QuestWorker.instance.GetCountComplete(m_curTab);
            var width = m_element.rtReward.rect.width;

            int i = 0;
            foreach (var reward in dbRewards)
            {
                var slot = (i == m_element.rtReward.childCount ? Instantiate(m_element.rtReward.GetChild(0), m_element.rtReward) : m_element.rtReward.GetChild(i)).GetComponent<ItemComponent>();
                slot.gameObject.SetActive(true);
                slot.SetItemData(reward);

                var line = (i == m_element.pGaugeBar.childCount ? Instantiate(m_element.pGaugeBar.GetChild(0), m_element.pGaugeBar) : m_element.pGaugeBar.GetChild(i));
                line.gameObject.SetActive(true);

                if (i == m_btnReward.Count)
                {
                    var rewardData = TableManager.questReward.GetQuestRewardData_Index(m_curTab, i);

                    var rt = slot.rt.anchoredPosition;
                    rt.x = width * (rewardData.target_value / 10f);
                    rt.y = 0;

                    slot.rt.anchoredPosition =
                    ((RectTransform)line).anchoredPosition = rt;

                    line.SetText("Text", rewardData.target_value);

                    m_btnReward.Add(slot.transform.GetComponent<Button>());
                    m_btnReward[i].onClick.AddListener(()
                        => OnButtonAsync_GaugeReward(rewardData).Forget());
                }
                i++;
            }

            for (; i < m_element.rtReward.childCount; i++)
            {
                m_element.rtReward.GetChild(i).gameObject.SetActive(false);
                m_element.pGaugeBar.GetChild(i).gameObject.SetActive(false);
            }
        }

        async UniTask OnButtonAsync_GaugeReward(TableQuestData _tableData)
        {

        }

        public override void Close()
        {
            QuestWorker.instance.SaveReddotTick();
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
            public TextMeshProUGUI txtTimer;
            public GaugeHelper gauge;
            public Transform pGaugeBar;
            public RectTransform rtReward;
            public ScrollRect scroll;

            public Transform pTabs;

            public void Initialize(Transform _transform)
            {
                txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
                txtTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/Timer/Text");
                gauge = _transform.GetComponent<GaugeHelper>("Panel/Gauge");
                pGaugeBar = gauge.transform.Find("Bar/img_bar");
                rtReward = (RectTransform)_transform.Find("Panel/Gauge/Rewards");
                scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");

                pTabs = _transform.Find("Panel/Tab");
            }

            public Transform panel => gauge.transform.parent;
        }
        #endregion VALIDATE
    }
}