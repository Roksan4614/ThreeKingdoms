using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HeroIconComponent : MonoBehaviour, IValidatable
{
    [SerializeField]
    public HeroInfoData data { get; private set; }

    UnityAction<HeroIconComponent> m_onClick;
    UnityAction<HeroIconComponent> m_onClickAction;

    private void Start()
    {
        m_element.btnHero.onClick.AddListener(() => m_onClick(this));
        m_element.btnAction?.onClick.AddListener(() => m_onClickAction(this));
    }

    public async UniTask SetHeroData(HeroInfoData _data
        , UnityAction<HeroIconComponent> _onClick
        , UnityAction<HeroIconComponent> _onClickAction)
    {
        if (_data.skin.Equals(data.skin))
            return;

        m_onClick = _onClick;
        m_onClickAction = _onClickAction;

        m_element.icon.parent.gameObject.SetActive(true);
        m_element.btnAction.gameObject.SetActive(false);
        m_element.txtLevel.text = _data.enchantLevel.ToString();
        m_element.txtName.text = _data.name;

        m_element.dimm.SetActive(_data.isMine == false);
        m_element.outline.color = _data.isMine ? Color.black : Color.gray;
        m_element.btnHero.interactable = _data.isMine;

        bool isFinded = false;
        for (int i = 0; i < m_element.icon.childCount; i++)
        {
            bool isValid = data.isActive && data.skin.Equals(_data.skin);
            m_element.icon.GetChild(i).gameObject.SetActive(isValid);

            if (isFinded == false && isValid == true)
                isFinded = isValid;
        }

        if (isFinded == false)
        {
            var prefab = await AddressableManager.instance.GetHeroIcon(_data.skin)
                .AttachExternalCancellation(destroyCancellationToken);

            if (prefab != null)
            {
                Instantiate(prefab, m_element.icon)
                    .AutoResizeParent()
                    .name = _data.skin;
            }
        }

        data = _data;
    }

    public void Disable()
    {
        m_element.txtLevel.text = "";
        m_element.txtName.text = "";
        m_element.icon.parent.gameObject.SetActive(false);
        m_element.btnAction.gameObject.SetActive(false);
        m_element.btnHero.interactable = false;
        data = default;
    }

    public void SetActiveButton(bool _isActive)
        => m_element.btnAction.gameObject.SetActive(_isActive);

    public void IsValide(string _keyHero)
        => data.key.Equals(_keyHero);

    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }

    [SerializeField]
    //[SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;
    [Serializable]
    public struct ElementData
    {
        public Transform icon;
        public Text txtName;
        public Text txtLevel;
        public Button btnHero;
        public Button btnAction;

        public GameObject dimm;
        public Image outline;

        public void Initialize(Transform _transform)
        {
            icon = _transform.Find("Panel/Icon/Panel");
            txtName = _transform.Find("txt_name").GetComponent<Text>();
            txtLevel = _transform.Find("txt_level").GetComponent<Text>();

            btnHero = _transform.GetComponent<Button>();
            btnAction = _transform.GetComponent<Button>("Panel/btn_action");

            dimm = icon.parent.Find("Dimm").gameObject;
            outline = icon.parent.GetComponent<Image>("Outline");
        }
    }
}
