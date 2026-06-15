using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;

public class ItemComponent : MonoBehaviour, IValidatable
{
    public TableItemData data { get; private set; }
    public RectTransform rt => (RectTransform)transform;

    private void Awake()
    {
        if (m_element.txtCount != null)
            m_element.txtCount.text = "";

        SetIconAsync(null, true).Forget();
        SetActiveRewardEffect(false);
    }

    public void SetItemData(TableItemData _itemData)
    {
        data = _itemData;

        gameObject.SetActive(true);

        m_element.icon.SetActive(_itemData.isActive);
        m_element.empty.SetActive(_itemData.isActive == false);
        if (_itemData.isActive == false)
        {
            txtCount = "";
            return;
        }

        //m_element.panel.gameObject.SetActive(false);

        if (_itemData.category == ItemCategoryType.Soul_Stone)
            SetIconAsync(_itemData.value, true).Forget();
        else
            SetIconAsync(_itemData.key.ToString(), false).Forget();

        txtCount = _itemData.count > 0 ? $"x{_itemData.count.AmountKMBT()}" : "";
    }

    async UniTask SetIconAsync(string _key, bool _isHero)
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
            {
                return;
            }

            var icon = Instantiate(result, m_element.iconPanel);
            icon.AutoResizeParent();
        }
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

    public string txtCount { set { if (m_element.txtCount != null) m_element.txtCount.text = value; } }

    public void SetCountText(long _count, bool _isRange = false)
        => txtCount = _count <= 1 ? "" : $"{(_isRange ? "~ " : "")}{_count.AmountKMBT()}";

    public void SetActivePanel(bool _isActive)
        => m_element.panel.gameObject.SetActive(_isActive);
    public void SetActiveBadge(bool _isActive)
        => m_element.badge.SetActive(_isActive);
    public void SetActiveDimm(bool _isActive)
        => m_element.dimm?.SetActive(_isActive);
    public void SetActiveRewardEffect(bool _isActive)
        => m_element.rewardEffect?.SetActive(_isActive);

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
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
            txtCount = panel.GetComponent<TextMeshProUGUI>("txt_count");

            badge = panel.Find("Badge")?.gameObject;
            dimm = iconPanel.parent.Find("Dimm")?.gameObject;

            rewardEffect = _transform.Find("RewardEffect")?.gameObject;
        }

        public GameObject icon => iconPanel.parent.gameObject;
    }
    #endregion VALIDATA
}
