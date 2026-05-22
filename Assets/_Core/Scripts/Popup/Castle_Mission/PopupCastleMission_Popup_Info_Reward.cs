using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class PopupCastleMission_Popup_Info_Reward : MonoBehaviour, IValidatable
{
    PopupCastleMission_Popup_Info_RewardItem m_rewardFixed => m_element.baseRewardItem;
    PopupCastleMission_Popup_Info_RewardItem m_rewardRandom;

    private void Awake()
    {
        m_rewardRandom = Instantiate(m_element.baseRewardItem, m_element.scroll.content).GetComponent<PopupCastleMission_Popup_Info_RewardItem>();
        m_element.baseRewardItem.SetTitle(true);
        m_rewardRandom.SetTitle(false);
        m_rewardRandom.name = "Random";
    }

    public void SetRewardList(Data_Castle_Mission.CastleMissionData _missionData, float _percent)
    {
        var dbGroup = TableManager.castleMissonReward.GetReward(_missionData).GroupBy(x => x.unlock_pct == 0).ToDictionary(x => x.Key, x => x);

        // 확정 보상
        m_rewardFixed.SetReward((int)_percent, dbGroup[true].ToArray());

        // 잠긴 보상
        m_rewardRandom.SetReward((int)_percent,
            dbGroup[false].OrderByDescending(x => x.unlock_pct <= _percent).ThenByDescending(x => x.unlock_pct).ToArray());
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
