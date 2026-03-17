using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEditor.Build;
using UnityEngine;

public class ItemComponent : MonoBehaviour, IValidatable
{
    public RectTransform rt => (RectTransform)transform;

    private void Awake()
    {
        m_element.txtCount.text = "";
        SetIconAsync(null, true).Forget();
    }

    public void SetItemData(string _key, long _count, bool _isHero)
    {
        gameObject.SetActive(true);
        SetIconAsync(_key, _isHero).Forget();
        m_element.txtCount.text = $"x{_count.AmountKMBT()}";
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

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtCount;
        public Transform iconPanel;

        public void Initialize(Transform _transform)
        {
            iconPanel = _transform.Find("Panel/Icon/Panel");
            txtCount = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_count");
        }
    }
    #endregion VALIDATA
}
