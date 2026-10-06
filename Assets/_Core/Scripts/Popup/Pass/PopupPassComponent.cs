using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Pass
{
    public class PopupPassComponent : BasePopupComponent
    {
        PopupPassComponent() : base(PopupType.Pass) { }

        TabType m_curTab = TabType.NONE;
        Dictionary<TabType, ButtonHelper> m_tabs = new();

        PopupBuyComponent m_popupBuy;

        private void Start()
        {
            for (var i = TabType.NONE + 1; i < TabType.MAX; i++)
            {
                TabType tab = i;
                int idx = (int)i;

                m_tabs.Add(tab, m_element.btnTap[idx]);
                m_tabs[tab].onClick.AddListener(() => SetTab(tab));

                m_tabs[tab].text = TableManager.stringTable.GetString($"UI_PASS_{tab.ToString().ToUpper()}");
            }

            m_element.reward.gameObject.SetActive(false);
            m_element.mission.gameObject.SetActive(false);

            m_element.mission.actionComplete = _slot => OnButtonAsync_Complete(_slot).Forget();

            m_element.panel.gameObject.SetActive(false);

            //setlocalization
            {
                transform.SetTextTable("Panel/txt_title", "UI_PASS_TITLE");
                transform.SetText("Panel/Info/txt_desc", TableManager.stringTable
                    .GetStringFormat("UI_PASS_DESC", TableManager.stringHero.GetName(CharacterName.LiuBei)));
                m_element.btnPass.text = TableManager.stringTable.GetString("UI_PASS_BUY_DESC");
            }

            m_element.panel.gameObject.SetActive(false);

            if (DataManager.pass.isPaid)
            {
                m_element.btnPass.interactable = false;
                m_element.txtCost.text = TableManager.stringTable.GetString("UI_PASS_RUNNING");
            }
            else
            {
                m_element.btnPass.interactable = true;
                m_element.txtCost.text = TableManager.shopProduct.GetBattlePass().price.AmountKMBT(_isMBT: true);
            }

            m_element.btnPass.onClick.AddListener(() => OnButtonAsync_BuyPass().Forget());
            m_element.reward.actionPass = () => OnButtonAsync_BuyPass().Forget();

            Signal.instance.Pass_UpdateQuest.connect = SlotPassUpdateQuest;
            Signal.instance.Buy_Item.connectLambda = new(this, _product =>
            {
                if (_product.product_type == ShopProductType.Pass)
                {
                    m_element.reward.InitializeScroll();
                    m_element.btnPass.interactable = false;
                    m_element.txtCost.text = TableManager.stringTable.GetString("UI_PASS_RUNNING");
                }
            });

            Utils.WaitEscape(this, () =>
            {
                if (m_popupBuy?.gameObject.activeSelf == true)
                {
                    m_popupBuy.Close();
                    return;
                }
                Close();
            }, _isMenuPopup: true);
        }

        public override void OpenPopup(params object[] _args)
        {
            gameObject.SetActive(true);
            OpenPopupAsync().Forget();
        }

        private void OnDisable()
        {
            if (m_popupBuy != null)
            {
                Destroy(m_popupBuy.gameObject);
                m_popupBuy = null;
            }
        }

        async UniTask OpenPopupAsync()
        {
            await UniTask.Yield();

            Utils.SetActivePunch(m_element.panel, true);
            SetTab(TabType.Reward);

            RefreshLevelXP();
        }

        async UniTask OnButtonAsync_BuyPass()
        {
            if (DataManager.pass.isPaid == true)
                return;

            var productPass = TableManager.shopProduct.GetBattlePass();

            // 유료재화가 부족합니다. 구매하시겠습니까?
            if (productPass.isEnoughCurrency == false)
            {
                var itemName = TableManager.item.GetItemData(ItemKey.GoldPaid).name;
                itemName = KoreanHelper.AppendJosa(itemName, KoreanHelper.JosaType.IgA, "[{0}]");
                var result = await PopupManager.instance.OpenModalAsync(
                    TableManager.alertString.GetStringFormat("MODAL_BUY_CURRENCY_NOT_ENOUGH", itemName), _posPointer: m_element.reward.posPointer);

                if (result == StatusType.Success)
                    DataManager.shop.OpenURL_GoldPaid(10000);

                return;
            }

            Utils.SetActivePunch(m_element.panel, false);
            await UniTask.WaitForSeconds(.1f);

            if (m_popupBuy == null)
                m_popupBuy = await PopupManager.instance.OpenPopupAsync<PopupBuyComponent>(PopupType.Buy, productPass);
            else
                m_popupBuy.OpenPopup(productPass);

            if (await m_popupBuy.WaitAsync() == StatusType.Success)
            {
                m_popupBuy.SetResult(await DataManager.shop.API_BuyItemAsync(productPass));
                DataManager.pass.SetBuyBattlePass();
                Signal.instance.Buy_Item.Emit(productPass);
            }

            Utils.SetActivePunch(m_element.panel, true);
        }

        void RefreshLevelXP()
        {
            int level = DataManager.pass.level;
            int exp = DataManager.pass.exp;

            m_element.txtLevelXP.text = level.ToString();

            var questData = TableManager.passReward.GetRewardData(level);
            m_element.gaugeXP.textAmount = $"{exp} / {questData.exp}";
            m_element.gaugeXP.fillAmount = exp / (float)questData.exp;
        }

        void SetTab(TabType _tab, bool _isForce = false)
        {
            if (m_curTab == _tab && _isForce == false)
                return;

            m_curTab = _tab;

            bool isReward = _tab == TabType.Reward;
            bool isMission = _tab == TabType.Mission;

            m_element.reward.gameObject.SetActive(isReward);
            m_element.mission.gameObject.SetActive(isMission);

            m_tabs[TabType.Reward].SetDrawSelect(isReward);
            m_tabs[TabType.Mission].SetDrawSelect(isMission);
        }

        async UniTask OnButtonAsync_Complete(PopupPass_Mission_Group_Slot _slot)
        {
            if (_slot.data.isComplete == true)
            {
                await DataManager.pass.API_QuestComplete(_slot.data.idx);
                RefreshLevelXP();
            }
            else
            {
                var type = _slot.data.tableData.key;
                StatusType result = StatusType.Wait;
                switch (type)
                {
                    case QuestType.Login:
                        break;
                    case QuestType.EnemyKill:
                    case QuestType.StageBossKill:
                        {
                            result = await PopupManager.instance.OpenModalAsync_Table("MODAL_MOVE_NAVI");
                            if (result == StatusType.Success)
                            {
                                Close();
                                LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Hero);
                            }
                        }
                        break;
                    default:
                        {
                            if (type == QuestType.AdsWatch)
                                result = await PopupManager.instance.OpenModalAsync_Table("MODAL_AD_SHOW");
                            else
                                result = await PopupManager.instance.OpenModalAsync_Table("MODAL_MOVE_NAVI");

                            if (result == StatusType.Success)
                            {
                                //먼저 꺼주기 위해서.. HOST가 꺼지면서 다른 팝업과 겹침 ㅜㅜ
                                Close();
                                await UniTask.WaitForSeconds(.1f);
                                await QuestWorker.instance.NavigationAsync(type, false);
                            }
                        }
                        break;
                }
            }
        }

        void SlotPassUpdateQuest(QuestType _questType)
        {
            m_element.mission.UpdateQuest(_questType);
        }

        public override void Close()
        {
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
            public Transform pHost;
            public ButtonHelper btnPass;
            public TextMeshProUGUI txtCost;
            public TextMeshProUGUI txtDescPass;
            public GaugeHelper gaugeXP;
            public TextMeshProUGUI txtLevelXP;

            public PopupPass_Reward reward;
            public PopupPass_Mission mission;

            public ButtonHelper[] btnTap;

            public void Initialize(Transform _transform)
            {
                txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title");
                txtTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/Timer/Text");
                pHost = _transform.Find("Panel/Host");
                btnPass = _transform.GetComponent<ButtonHelper>("Panel/Info/btn_pass");
                txtCost = _transform.GetComponent<TextMeshProUGUI>("Panel/Info/btn_pass/txt_cost");
                txtDescPass = _transform.GetComponent<TextMeshProUGUI>("Panel/Info/txt_desc");
                gaugeXP = _transform.GetComponent<GaugeHelper>("Panel/Info/Gauge_XP");
                txtLevelXP = gaugeXP.transform.GetComponent<TextMeshProUGUI>("Level/Text");

                reward = _transform.GetComponent<PopupPass_Reward>("Panel/Reward");
                mission = _transform.GetComponent<PopupPass_Mission>("Panel/Mission");

                btnTap = _transform.Find("Panel/Tab").GetComponentsInChildren<ButtonHelper>();
            }

            public Transform panel => txtTitle.transform.parent;
        }
        #endregion VALIDATE

        enum TabType
        {
            NONE = -1,

            Reward,
            Mission,

            MAX
        }
    }
}