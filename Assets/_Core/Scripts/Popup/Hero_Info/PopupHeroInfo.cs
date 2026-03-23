using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo : BasePopupComponent
{
    PopupHeroInfo() : base(PopupType.Hero_HeroInfo) { }

    HeroInfoData m_heroInfoData;

    CharacterComponent m_character;
    public bool isNeedUpdate { get; private set; }

    void Start()
    {
        //m_element.btnCharacter.onClick.AddListener(() => m_character.anim.Play(CharacterAnimType.Attack));

        m_element.btnStatus.onClick.AddListener(() => m_element.popupStatus.Open());

        for (int i = 0; i < m_element.popup.childCount; i++)
            m_element.popup.GetChild(i).gameObject.SetActive(false);

        Utils.WaitEscape(this, Close, _token: destroyCancellationToken);
    }

    private void OnEnable()
    {
        Utils.SetActivePunch(m_element.panel, true, false);
    }

    // HeroInfoData
    public override void OpenPopup(params object[] _args)
    {
        if (_args.Length > 0)
            SetHeroInfoDataAsync((HeroInfoData)_args[0]).Forget();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_data"></param>
    /// <param name="_isJustWatch">오로지 확인용. 승급하기 강화하기 같은거 없음</param>
    /// <returns></returns>
    public async UniTask SetHeroInfoDataAsync(HeroInfoData _data, bool _isJustWatch = false)
    {
        isNeedUpdate = false;
        gameObject.SetActive(true);

        if (_data.key == m_heroInfoData.key)
            return;

        m_heroInfoData = _data;

        var dbHeroData = TableManager.hero.Get(_data.key);

        // FRONT PANEL
        var key = $"{dbHeroData.regionType}_{_data.key}".ToUpper();
        m_element.txtName.text = $"{TableManager.stringHero.GetString("NAME_" + key)}<size=80%><color=#888888> {TableManager.stringHero.GetString("COURTESY_" + key)}";
        m_element.txtDescTalk.text = dbHeroData.talk;

        // 고유 능력치
        for (int i = 0; i < m_element.stat.Count; i++)
        {
            var value = dbHeroData.stat[i];
            var txt = m_element.stat[i].content;
            txt.text = value.ToString();
            m_element.stat[i].title.alpha = txt.alpha = value >= 90 ? 1 : value >= 80 ? .9f : value >= 70 ? .8f : value >= 60 ? .7f : .6f;
        }

        // CHARACTER 
        {
            var parent = m_element.panelHero;
            bool isFinded = false;
            for (int i = 0; i < parent.childCount; i++)
            {
                var obj = parent.GetChild(i).gameObject;
                obj.SetActive(obj.name.Contains(_data.skin));

                if (isFinded == false && obj.activeSelf == true)
                    isFinded = true;
            }

            if (isFinded == false)
            {
                var heroCharacter = (await AddressableManager.instance.GetHeroCharacterAsync(_data.skin)).GetComponent<CharacterComponent>();

                m_character = Instantiate(heroCharacter, parent);
                m_character.name = _data.skin;
                m_character.transform.localPosition = Vector3.zero;

                m_character.DeleteElement();
            }
        }

        // 버튼확인용
        m_element.btnConfirm.onClick.AddListener(Close);
    }

    public async UniTask AutoCloseAsync(float _duration)
    {
        m_element.btnEnchant.transform.parent.gameObject.SetActive(false);
        m_element.btnConfirm.transform.gameObject.SetActive(true);

        string key = "{0}초후_닫힘._터치하면_취소됩니다.";

        DateTime dtClose = DateTime.Now.AddSeconds(_duration);
        while (dtClose > DateTime.Now)
        {
            var timer = (dtClose - DateTime.Now).TotalSeconds;
            m_element.txtTimer_AutoClose.text = string.Format(key, Utils.MSpace($"{timer:0.0}"));

            if (ControllerManager.isClick)
            {
                m_element.txtTimer_AutoClose.text = "";
                return;
            }

            await UniTask.WaitForEndOfFrame();
        }

        Close();
    }

    public override void Close()
    {
        CloseAsync().Forget();
    }

    async UniTask CloseAsync()
    {
        await Utils.SetActivePunchAsync(m_element.panel, false, false);
        gameObject.SetActive(false);
    }

    public override void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;
    [Serializable]
    struct ElementData
    {
        public Transform panel;
        public Transform panelHero;
        //public Button btnCharacter;
        public Button btnStatus;

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtDescTalk;

        public PopupHeroInfo_PopupStatus popupStatus;

        public Transform popup;

        public ButtonHelper btnEnchant;
        public ButtonHelper btnUpgrade;
        public ButtonHelper btnConfirm;
        public TextMeshProUGUI txtTimer_AutoClose;

        public List<EntryData> stat;
        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
            panelHero = panel.Find("btn_character").GetChild(0);
            //btnCharacter = panel.GetComponent<Button>("btn_character");

            var frontPanel = panel.Find("FrontPanel");
            btnStatus = frontPanel.GetComponent<Button>("btn_status");
            txtName = frontPanel.GetComponent<TextMeshProUGUI>("txt_name");
            txtDescTalk = frontPanel.GetComponent<TextMeshProUGUI>("txt_desc");

            popup = _transform.Find("Popup");
            popupStatus = popup.GetComponent<PopupHeroInfo_PopupStatus>("Status");

            var pStat = panel.Find("Stat");
            stat = new();
            for (int i = 0; i < pStat.childCount; i++)
            {
                var item = pStat.GetChild(i);
                stat.Add(new EntryData()
                {
                    title = item.GetComponent<TextMeshProUGUI>("txt_title"),
                    content = item.GetComponent<TextMeshProUGUI>("txt_content"),
                });
            }

            // BUTTON
            btnEnchant = panel.GetComponent<ButtonHelper>("Buttons/btn_enchant");
            btnUpgrade = panel.GetComponent<ButtonHelper>("Buttons/btn_upgrade");
            btnConfirm = panel.GetComponent<ButtonHelper>("btn_confirm");

            txtTimer_AutoClose = btnConfirm.transform.GetComponent<TextMeshProUGUI>("txt_timer");
        }

        //public Transform panelHero => btnCharacter.transform.GetChild(0);
    }

    [Serializable]
    public struct EntryData
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI content;
    }
}
