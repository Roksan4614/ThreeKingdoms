using System.Collections.Generic;
using System.Linq;
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

    public void SetTitle(bool _isFixed, string _title)
    {
        m_element.txtTitle.text = _title;

        if (_isFixed)
        {
            m_element.txtTitle.color = Color.white;
            m_element.bgTitle.SetActive(true);
        }
        else
        {
            m_element.txtTitle.fontStyle = FontStyles.Bold;
            m_element.txtTitle.color = Color.black;
            m_element.bgTitle.SetActive(false);
        }

        m_element.txtTitle.transform.parent.ForceRebuildLayout();
    }

    public void SetReward(int _percent, params TableCastleMissionRewardData[] _rewardData)
    {
        int i = 0;
        for (; i < _rewardData.Length; i++)
        {
            var rewardData = _rewardData[i];
            var item = i == m_element.parent.childCount ?
                Instantiate(m_element.baseItem, m_element.parent) :
                m_element.parent.GetChild(i).GetComponent<ItemComponent>();

            item.gameObject.SetActive(true);

            TableItemData itemData = new();
            itemData.key = rewardData.reward_key;
            itemData.value = rewardData.reward_value;
            item.SetItemData(itemData);

            bool isLock = _percent < rewardData.unlock_pct;
            item.SetActiveDimm(isLock);

            if (isLock)
                item.SetCountText(0);
            else
                //min max 차이가 있으면 range로.. 없으면 걍 max로
                item.SetCountText(rewardData.reward_max, rewardData.reward_max - rewardData.reward_min > 0);
        }

        for (; i < m_element.parent.childCount; i++)
            m_element.parent.GetChild(i).gameObject.SetActive(false);

        m_element.parent.ForceRebuildLayout();
    }

    public void SetReward_Result(params TableItemData[] _rewardData)
    {
        int i = 0;
        for (; i < _rewardData.Length; i++)
        {
            var rewardData = _rewardData[i];
            var item = i == m_element.parent.childCount ?
                Instantiate(m_element.baseItem, m_element.parent) :
                m_element.parent.GetChild(i).GetComponent<ItemComponent>();

            item.gameObject.SetActive(true);

            item.SetItemData(_rewardData[i]);

            //min max 차이가 있으면 range로.. 없으면 걍 max로
            //item.SetCountText(rewardData.reward_max, rewardData.reward_max - rewardData.reward_min > 0);
        }

        for (; i < m_element.parent.childCount; i++)
            m_element.parent.GetChild(i).gameObject.SetActive(false);

        m_element.parent.ForceRebuildLayout();
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
