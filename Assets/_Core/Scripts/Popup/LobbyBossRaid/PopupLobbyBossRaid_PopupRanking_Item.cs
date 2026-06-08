using TMPro;
using UnityEngine;

public class PopupLobbyBossRaid_PopupRanking_Item : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        m_element.Initialize(transform);
    }

    public void SetRankerInfo()
    {

    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        TextMeshProUGUI txtRank;
        TextMeshProUGUI txtPrevRank;

        Transform pIcon;

        TextMeshProUGUI txtNickname;
        TextMeshProUGUI txtPower;
        TextMeshProUGUI txtPoint;

        public void Initialize(Transform _transform)
        {
            txtRank = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_rank");
            txtPrevRank = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_rank/txt_prev");
            txtNickname = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_nickname");
            txtPower = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_power");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_point");

            pIcon = _transform.Find("Panel/Icon");
        }
    }
    #endregion VALIDATE

}
