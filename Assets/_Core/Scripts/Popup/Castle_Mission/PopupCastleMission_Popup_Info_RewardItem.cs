using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupCastleMission_Popup_Info_RewardItem : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        if (m_element.baseItem == null)
            m_element.Initialize(transform);

        m_element.baseItem.transform.SetParent(transform);
        m_element.baseItem.gameObject.SetActive(false);
    }

    public void SetUnlock(bool _isUnlock)
    {
        m_element.txtTitle.color = Color.white;
        m_element.bgTitle.SetActive(true);
    }

    public void SetReward(params TableCastleMissionRewardData[] _rewardData)
    {
        int i = 0;

        foreach (var rewardData in _rewardData)
        {

        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;
        public GameObject bgTitle;

        public Transform parent;
        public ItemComponent baseItem;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Title/txt_title");
            bgTitle = _transform.Find("Title/BG").gameObject;

            parent = _transform.Find("Rewards");
            baseItem = parent.GetChild(0).GetComponent<ItemComponent>();
        }
    }
    #endregion VALIDATE

}
