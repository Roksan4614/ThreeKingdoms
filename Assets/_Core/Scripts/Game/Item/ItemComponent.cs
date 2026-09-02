using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;

public class ItemComponent : MonoBehaviour, IValidatable
{
    public ItemData data { get; private set; }
    public RectTransform rt => (RectTransform)transform;

    protected virtual void Awake()
    {
        txtCount = "";

        SetIconAsync(null, true).Forget();
        SetActiveRewardEffect(false);
    }

    public void SetItemData(ItemData _itemData)
    {
        data = _itemData;

        gameObject.SetActive(true);

        bool isActive = _itemData != null;

        m_element.icon.SetActive(isActive);
        m_element.empty.SetActive(isActive == false);
        if (isActive == false)
        {
            txtCount = "";
            return;
        }

        //m_element.panel.gameObject.SetActive(false);

        if (_itemData.category == ItemCategoryType.Soul_Stone)
        {
            if (_itemData.key == ItemType.dedicated_soul_stone)
            {
                SetIconAsync(_itemData.value, true, _iconHero =>
                {
                    SetIconAsync(_itemData.key.ToString(), false, _icon =>
                    {
                        _icon.SetParent(_iconHero);
                        _iconHero.gameObject.SetActive(true);
                    }).Forget();
                }).Forget();
            }
            else if (_itemData.key == ItemType.class_soul_stone)
                SetIconAsync($"{_itemData.key}_{_itemData.value}", false).Forget();
            else
                SetIconAsync(_itemData.key.ToString(), false).Forget();
        }
        else
            SetIconAsync(_itemData.key.ToString(), false).Forget();

        txtCount = _itemData.count > 0 ? $"x{_itemData.count.AmountKMBT()}" : "";
    }

    async UniTask SetIconAsync(string _key, bool _isHero, Action<Transform> _onComplete = null)
    {
        bool isFinded = false;
        for (int i = 0; i < m_element.iconPanel.childCount; i++)
        {
            var icon = m_element.iconPanel.GetChild(i).gameObject;

            icon.SetActive(icon.name.Equals(_key));
            if (isFinded == false)
                isFinded = icon.activeSelf;
        }

        if (isFinded == false && _key.IsActive())
        {
            var result = await AddressableManager.instance.GetIconAsync(_key, _isHero);
            if (result == null)
                return;

            var icon = Instantiate(result, m_element.iconPanel);
            icon.transform.SetAsFirstSibling();
            icon.AutoResizeParent().name = _key;

            _onComplete?.Invoke(icon.transform);
        }
    }

    public void SetIconAutoResize()
    {

    }

    public void MoveFinished()
    {
        Utils.SetActivePunch(m_element.panel, true);
        m_element.iconPanel.parent.gameObject.SetActive(true);

        SetActiveBadge(data.isNew);
        SetActiveRewardEffect(false);
    }

    public void SetSoulCount(long _count = 0)
    {
        SetActivePanel(true);
        //m_element.panel.gameObject.SetActive(true);
        m_element.iconPanel.parent.gameObject.SetActive(false);

        txtCount = _count == 0 ? "" : $"x{_count.AmountKMBT()}";
        SetActiveBadge(false);
    }

    public string txtCount
    {
        set
        {
            if (m_element.txtCount == null)
                return;
            if (value.IsActive())
            {
                m_element.count.SetActive(true);
                m_element.txtCount.text = value;
                m_element.count.transform.ForceRebuildLayout();
            }
            else
                m_element.count.SetActive(false);
        }
    }

    public void SetCountText(long _count, bool _isRange = false)
        => txtCount = _count <= 1 ? "" : $"{(_isRange ? "~ " : "")}{_count.AmountKMBT()}";

    public void SetActivePanel(bool _isActive)
        => m_element.panel.gameObject.SetActive(_isActive);
    public void SetActiveBadge(bool _isActive)
        => m_element.badge.SetActive(_isActive);
    public void SetActiveDimm(bool _isActive)
    {
        if (m_element.dimm)
            m_element.dimm.SetActive(_isActive);
    }
    public void SetActiveRewardEffect(bool _isActive)
    {
        if (m_element.rewardEffect)
            m_element.rewardEffect.SetActive(_isActive);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    protected ElementData m_element;

    [Serializable]
    protected struct ElementData
    {
        public Transform panel;
        public GameObject empty;

        public TextMeshProUGUI txtCount;
        public Transform iconPanel;

        public GameObject badge;
        public GameObject dimm;

        public GameObject rewardEffect;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");

            empty = panel.Find("Empty").gameObject;

            iconPanel = panel.Find("Icon/Panel");
            txtCount = panel.GetComponent<TextMeshProUGUI>("Count/Text");

            badge = panel.Find("Badge")?.gameObject;
            dimm = iconPanel.parent.Find("Dimm")?.gameObject;

            rewardEffect = _transform.Find("RewardEffect")?.gameObject;
        }

        public GameObject icon => iconPanel.parent.gameObject;
        public GameObject count => txtCount.transform.parent.gameObject;
    }
    #endregion VALIDATA
}
