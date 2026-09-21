using Cysharp.Threading.Tasks;
using System.Collections.Generic;
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
                transform.SetTextTable("Panel/Info/txt_desc", "UI_PASS_DESC");
                m_element.btnPass.text = TableManager.stringTable.GetString("UI_PASS_BUY_DESC");
            }
        }

        public override void OpenPopup(params object[] _args)
        {
            gameObject.SetActive(true);
            OpenPopupAsync().Forget();
        }

        async UniTask OpenPopupAsync()
        {
            m_element.panel.gameObject.SetActive(false);
            await DataManager.pass.InitializeAsync();

            m_element.panel.gameObject.SetActive(true);
            SetTab(TabType.Reward);

            RefreshLevelXP();
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
            // todo 완료시켜야 해

            RefreshLevelXP();
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