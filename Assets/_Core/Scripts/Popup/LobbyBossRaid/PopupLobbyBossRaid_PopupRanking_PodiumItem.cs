using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyBossRaid_PopupRanking_PodiumItem : MonoBehaviour, IValidatable
{
    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Transform pHero;

        public Button btnUserInfo;
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtPoint;

        public void Initialize(Transform _transform)
        {
            pHero = _transform.Find("Panel/Hero");

            btnUserInfo = _transform.GetComponent<Button>("Panel/btn_userInfo");
            txtName = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_name");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_point");
        }
    }
    #endregion VALIDATE

}
