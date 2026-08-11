using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Tournament
{
    public class PopupTournament_RewardInfo_Slot : MonoBehaviour, IValidatable
    {
        public void SetRewardData(TableTournamentRewardData _rewardData)
        {
            m_element.txtTier.text = _rewardData.tierName;
            m_element.txtName.text = _rewardData.desc;

            var content = m_element.scroll.content;
            for (int i = 1; i < _rewardData.rewards.Count; i++)
                Instantiate(content.GetChild(0), content);

            for (int i = 0; i < _rewardData.rewards.Count; i++)
                content.GetChild(i).GetComponent<ItemComponent>().SetItemData(_rewardData.rewards[i]);
        }

        private void OnEnable()
        {
            m_element.scroll.content.anchoredPosition = Vector2.zero;
        }

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public ScrollRect scroll;
            public Image iconTier;
            public TextMeshProUGUI txtTier;
            public TextMeshProUGUI txtName;

            public void Initialize(Transform _transform)
            {
                scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");
                iconTier = _transform.GetComponent<Image>("Panel/IconTier");
                txtTier = _transform.GetComponent<TextMeshProUGUI>("Panel/IconTier/txt_tier");
                txtName = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_name");
            }
        }
        #endregion VALIDATE

    }
}
