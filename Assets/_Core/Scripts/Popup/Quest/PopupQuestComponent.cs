using Cysharp.Threading.Tasks;
using DG.Tweening;
using Rev9.ContentsMarket;
using System;
using System.Collections.Generic;
using System.Threading;
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

        CancellationTokenSource m_cts;

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
            Signal.instance.Quest_UpdateComplete.connect = SlotUpdateComplete;

            Utils.WaitEscape(this, Close, _isMenuPopup: true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                for (var i = QuestType.NONE + 1; i < QuestType.MAX; i++)
                    QuestWorker.instance.AddCount(i);
            }
        }

        private void OnDisable()
        {
            m_cts = m_cts.ReleaseCTS();
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
            SlotUpdateComplete(m_curTab);

            m_element.scroll.velocity =
            m_element.scroll.content.anchoredPosition = Vector2.zero;

            TimerAsync().Forget();
        }

        async UniTask TimerAsync()
        {
            m_cts = m_cts.ReleaseCTS(true);
            var token = m_cts.Token;

            var utc = Utils.GetUTC();
            DateTime endTime;

            int addHours = 0;

            if (m_curTab == QuestCategoryType.daily)
                endTime = utc.Date.AddDays(1).AddHours(addHours);
            else
                endTime = Utils.GetNextMidnight(DayOfWeek.Monday);

            TimeSpan ts = endTime - Utils.GetUTC();
            int prevSec = -1;
            while (ts.TotalSeconds > 0)
            {
                if (ts.TotalSeconds <= 10f || prevSec != ts.Seconds)
                {
                    prevSec = ts.Seconds;
                    m_element.txtTimer.text = ts.ToRemainTime(22);
                }

                await UniTask.NextFrame(token);
                ts = endTime - Utils.GetUTC();
            }

            m_element.txtTimer.text = "_정산중_";
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
            if (ThreeKingdoms.Client.Server.PrototypeContentNotice.ShowIfServer()) return;
            if (_questData.isComplete == true)
            {
                if (await QuestWorker.instance.API_ReceiveReward(_questData) == true)
                {
                    _slot.UpdateStatus(false);
                    _slot.transform.SetAsLastSibling();

                    RewardWorker.instance.Run(_slot.trnsRewardIcon.position, _itemData: _questData.data.itemData);
                }
            }
            else
            {
                var result = await PopupManager.instance.OpenModalAsync("이동_하시겠습니까?");

                if (result != StatusType.Success)
                    return;

                // NAVIGATION
                switch (_questData.key)
                {
                    case QuestType.tournament_play:
                        PopupManager.instance.OpenPopup(PopupType.LobbyTournament);
                        break;
                    case QuestType.raid_play:
                        PopupManager.instance.OpenPopup(PopupType.LobbyBossRaid);
                        break;
                    case QuestType.gacha_proceed:
                        LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Summon);
                        break;
                    case QuestType.rice_claim:
                    case QuestType.gold_claim:
                    case QuestType.office_dispatch:
                        LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Castle);
                        break;
                    case QuestType.daily_dungeon_play:
                        LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Boss);
                        break;
                    case QuestType.item_buy:
                        LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Shop);
                        break;
                    case QuestType.item_use:
                        PopupManager.instance.OpenPopup(PopupType.Inventory);
                        break;
                    default:
                        return;
                }

                Close();
            }
        }

        void SlotUpdateStatus(QuestInfoData _questData)
        {
            if (gameObject.activeSelf == false)
                return;

            if (_questData == null)
            {
                var tab = m_curTab;
                m_curTab++;
                SetTab(tab);
                return;
            }

            if (_questData.type != m_curTab)
                return;

            var slot = m_slots.Find(x => x.questData == _questData);
            slot.UpdateStatus(false);
        }

        void SlotUpdateComplete(QuestCategoryType _category)
        {
            if (gameObject.activeSelf == false)
                return;

            if (_category > QuestCategoryType.NONE && _category != m_curTab)
                return;

            int countComplete = QuestWorker.instance.GetCountComplete(m_curTab);

            m_element.gauge.fillAmount = countComplete / 10f;
            m_element.txtCountComplete.text = countComplete.ToString();

            SetGaugeRewardList();
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

                var rewardData = TableManager.questReward.GetQuestRewardData_Index(m_curTab, i);
                bool isReceived = QuestWorker.instance.IsReceivedGaugeReward(m_curTab, rewardData.target_value);

                // 이미 받았으면 흐리게
                slot.GetComponent<CanvasGroup>().alpha = isReceived ? .5f : 1f;

                var rtSlot = (RectTransform)slot.transform;
                rtSlot.rotation = Quaternion.identity;
                rtSlot.DOKill();

                // 아직 안받았으면 왔다갔다 하자
                if (isReceived == false && rewardData.target_value <= countComplete)
                {
                    float duration = .5f;
                    rtSlot.DORotate(new Vector3(0, 0, -3), duration * 0.5f).SetEase(Ease.OutSine).OnComplete(() =>
                    {
                        rtSlot.DORotate(new Vector3(0, 0, 3), duration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo).ToUniTask(cancellationToken: destroyCancellationToken);
                    }).ToUniTask(cancellationToken: destroyCancellationToken);
                }

                if (i == m_btnReward.Count)
                {
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

        async UniTask OnButtonAsync_GaugeReward(TableQuestData _tableRewardData)
        {
            if (ThreeKingdoms.Client.Server.PrototypeContentNotice.ShowIfServer()) return;
            int countComplete = QuestWorker.instance.GetCountComplete(m_curTab);

            if (_tableRewardData.target_value > countComplete)
                return;

            var dbRewards = TableManager.questReward.GetQuestRewardData(m_curTab);

            List<ItemData> rewardItems = new();
            List<int> targets = new();
            foreach (var reward in dbRewards)
            {
                if (countComplete >= reward.target_value && QuestWorker.instance.IsReceivedGaugeReward(m_curTab, reward.target_value) == false)
                {
                    targets.Add(reward.target_value);
                    rewardItems.Add(reward.itemData);
                }
            }

            if (targets.Count == 0)
                return;

            if (await QuestWorker.instance.API_ReceiveGaugeReward(m_curTab, targets.ToArray()) == true)
            {
                SetGaugeRewardList();

                int i = 0;
                foreach (var target in targets)
                {
                    var idx = dbRewards.FindIndex(x => x.target_value == target);
                    if (idx > -1)
                    {
                        var pos = m_element.rtReward.GetChild(idx).transform.position;
                        RewardWorker.instance.Run(pos, _itemData: rewardItems[i]);
                    }

                    i++;
                }
            }
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
            public TextMeshProUGUI txtCountComplete;
            public GaugeHelper gauge;
            public Transform pGaugeBar;
            public RectTransform rtReward;
            public ScrollRect scroll;

            public Transform pTabs;

            public void Initialize(Transform _transform)
            {
                txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
                txtTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/Timer/Text");
                txtCountComplete = _transform.GetComponent<TextMeshProUGUI>("Panel/Gauge/Count/Text");
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