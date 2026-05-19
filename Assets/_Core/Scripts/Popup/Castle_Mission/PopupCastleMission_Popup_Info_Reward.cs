using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class PopupCastleMission_Popup_Info_Reward : MonoBehaviour, IValidatable
{
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
