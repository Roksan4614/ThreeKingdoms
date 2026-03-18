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

        SetIconAsync(_itemData.value, _itemData.key == ItemType.Stone_Soul).Forget();
        m_element.txtCount.text = $"x{_itemData.count.AmountKMBT()}";
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
    }

    public void SetSoulCount(int _count)
    {
        m_element.panel.gameObject.SetActive(true);
        m_element.iconPanel.parent.gameObject.SetActive(false);

        m_element.txtCount.text = $"x{_count.AmountKMBT()}";
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

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");

            iconPanel = _transform.Find("Panel/Icon/Panel");
            txtCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_count");
        }
    }
    #endregion VALIDATA
}
