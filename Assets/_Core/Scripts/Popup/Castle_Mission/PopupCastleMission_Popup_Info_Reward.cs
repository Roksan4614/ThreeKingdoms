using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PopupCastleMission_Popup_Info_Reward : MonoBehaviour, IValidatable
{
    PopupCastleMission_Popup_Info_RewardItem m_rewardFixed => m_element.baseRewardItem;
    PopupCastleMission_Popup_Info_RewardItem m_rewardRandom;

    private void Awake()
    {
        m_rewardRandom = Instantiate(m_element.baseRewardItem, m_element.scroll.content).GetComponent<PopupCastleMission_Popup_Info_RewardItem>();
        m_element.baseRewardItem.SetTitle(true, "확정_보상");
        m_rewardRandom.SetTitle(false, "획득_가능_보상");
        m_rewardRandom.name = "Random";
    }

    public void SetTitleResult()
    {
        m_element.baseRewardItem.SetTitle(true, "확정_보상");
        m_rewardRandom.SetTitle(false, "획득_가능_보상");
    }

    public void SetRewardList(Data_Castle_Mission.CastleMissionData _missionData, float _percent)
    {
        var dbGroup = TableManager.castleMissonReward.GetReward(_missionData).GroupBy(x => x.unlock_pct == 0).ToDictionary(x => x.Key, x => x);

        // 확정 보상
        m_rewardFixed.SetReward(100, dbGroup[true].ToArray());

        // 잠긴 보상
        m_rewardRandom.SetReward((int)_percent, 
            dbGroup[false].OrderByDescending(x => x.unlock_pct <= _percent).ThenByDescending(x => x.unlock_pct).ToArray());
    }

    public void SetReward_ResultFixed(params TableCastleMissionRewardData[] _rewardData)
        => m_rewardFixed.SetReward(100, _rewardData);
    public void SetReward_ResultRandom(params TableCastleMissionRewardData[] _rewardData)
        => m_rewardRandom.SetReward(100, _rewardData);

    //public void SetReward_ResultRandom(params ItemData[] _rewardData)
    //    => m_rewardFixed.SetReward_Result(_rewardData);

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
