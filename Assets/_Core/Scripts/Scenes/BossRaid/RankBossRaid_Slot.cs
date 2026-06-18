using TMPro;
using UnityEngine;

public class RankBossRaid_Slot : MonoBehaviour, IValidatable
{
    public RectTransform rt => (RectTransform)transform;

    public void SetRankData(Data_BossRaid.BossRaidRankerUserData _userData)
    {
        int _countMax = DataManager.bossRaid.rankNow.Count;

        m_element.txtRank.text = $"{(_userData.point == 0 ? "-" : _userData.rank)}\n<color=#{(_userData.uid == DataManager.userInfo.uid ? "CBCBCB" : "5C5C5C")}><size=70%>({(_userData.point == 0 ? "100.00" : (_userData.rank - 1) / (float)_countMax):0.00}%)</size></color>";
        m_element.txtNickname.text = _userData.nickname;
        m_element.txtPoint.text = _userData.point.AmountKMBT();
    }

    public void SetEmpty()
        => m_element.txtPoint.text = m_element.txtNickname.text = m_element.txtRank.text = "-";


    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtRank;
        public TextPanelHelper txtNickname;
        public TextMeshProUGUI txtPoint;
        public void Initialize(Transform _transform)
        {
            txtRank = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_rank");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_point");
            txtNickname = _transform.GetComponent<TextPanelHelper>("Panel/txt_nickname");
        }
    }
    #endregion VALIDATE

}
