using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class PopupCastleMission_Popup_Info_Reward : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        m_element.baseRewardItem.transform.SetParent(m_element.scroll.viewport);
        m_element.baseRewardItem.gameObject.SetActive(false);
    }

    public void SetRewardList(Data_Castle_Mission.CastleMissionData _missionData, float _percent)
    {
        var db = TableManager.castleMissonReward.GetReward(_missionData).ToList();

        var content = m_element.scroll.content;
        int i = 0;

        // È¹µæº¸»ó
        {
            var item = i == content.childCount ?
                Instantiate(m_element.baseRewardItem, content) :
                content.GetChild(i).GetComponent<PopupCastleMission_Popup_Info_RewardItem>();

            item.gameObject.SetActive(true);
            item.SetReward(db.Where(x => x.unlock_pct <= _percent).ToArray());
            item.SetUnlock(true);
            i++;
        }

        // Àá±äº¸»ó
        {
            var dbLock = db.Where(x => x.unlock_pct > _percent).GroupBy(x => x.unlock_pct).ToList();

            for (; i < dbLock.Count; i++)
            {
                var item = i == content.childCount ?
                    Instantiate(m_element.baseRewardItem, content) :
                    content.GetChild(i).GetComponent<PopupCastleMission_Popup_Info_RewardItem>();

                item.gameObject.SetActive(true);

                item.SetReward(dbLock[i].ToArray());
            }
        }

        for (; i < content.childCount; i++)
            content.GetChild(i).gameObject.SetActive(false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ScrollRect scroll;
        public PopupCastleMission_Popup_Info_RewardItem baseRewardItem;

        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<ScrollRect>("Scroll");
            baseRewardItem = scroll.content.GetComponent<PopupCastleMission_Popup_Info_RewardItem>("Item");
        }
    }
    #endregion VALIDATE

}
