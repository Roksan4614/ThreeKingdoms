using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEditor.Build;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class ItemComponent : MonoBehaviour, IValidatable
{
    public TableItemData data { get; private set; }
    public RectTransform rt => (RectTransform)transform;

    private void Awake()
    {
        m_element.txtCount.text = "";
        SetIconAsync(null, true).Forget();
    }

    public void SetItemData(TableItemData _itemData)
    {
        data = _itemData;

        gameObject.SetActive(true);
        m_element.panel.gameObject.SetActive(false);

        bool isHero = _itemData.key == ItemType.Stone_Soul;
        SetIconAsync(_itemData.value, isHero).Forget();
        m_element.txtCount.text = _itemData.count > 0 ? $"x{_itemData.count.AmountKMBT()}" : "";
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
                return;

            var icon = Instantiate(result, m_element.iconPanel);
            icon.AutoResizeParent();
        }
    }

    public void MoveFinished()
    {
        Utils.SetActivePunch(m_element.panel, true);
        m_element.panel.gameObject.SetActive(true);
        m_element.iconPanel.parent.gameObject.SetActive(true);

        m_element.badge.SetActive(data.isNew);
    }

    public void SetSoulCount(long _count = 0)
    {
        m_element.panel.gameObject.SetActive(true);
        m_element.iconPanel.parent.gameObject.SetActive(false);

        m_element.txtCount.text = _count == 0 ? "" : $"x{_count.AmountKMBT()}";
        m_element.badge.SetActive(false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform panel;

        public TextMeshProUGUI txtCount;
        public Transform iconPanel;

        public GameObject badge;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");

            iconPanel = panel.Find("Icon/Panel");
            txtCount = panel.GetComponent<TextMeshProUGUI>("txt_count");

            badge = panel.Find("Badge").gameObject;
        }
    }
    #endregion VALIDATA
}
