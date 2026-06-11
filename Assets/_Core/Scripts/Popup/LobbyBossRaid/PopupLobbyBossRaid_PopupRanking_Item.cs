using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupLobbyBossRaid_PopupRanking_Item : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        m_element.Initialize(transform);
    }

    public async UniTask SetRankerInfoAsync(
        PopupLobbyBossRaid_PopupRanking.TabType _tabType,
        Data_BossRaid.BossRaidRankerUserData _rankerData,
        UnityAction<Data_BossRaid.BossRaidRankerUserData> _callback)
    {
        if (m_element.button != null)
        {
            m_element.button.onClick.RemoveAllListeners();
            m_element.button.onClick.AddListener(() => _callback?.Invoke(_rankerData));
        }

        // 닉네임
        m_element.txtNickname.text = _rankerData.nickname;

        // 현재 랭킹
        m_element.txtRank.text = _rankerData.rank.ToString();

        // 이전 랭킹 차이
        var diff = _rankerData.prevRank - _rankerData.rank;
        m_element.txtPrevRank.text = diff == 0 ? "(-)" : $"<color=#{(diff > 0 ? Palette.htmlString_Up : Palette.htmlString_Down)}>({(diff > 0 ? "+" : "")}{diff})";

        // 배치 CP
        m_element.txtPower.text = $"cp{_rankerData.power:#,0}";

        // 포인트
        if (_tabType == PopupLobbyBossRaid_PopupRanking.TabType.Point)
            m_element.txtPoint.text = $"{_rankerData.point:#,0}p";
        else
            m_element.txtPoint.text = $"{_rankerData.point:#,0}";

        // 내꺼일 경우 배경 색 적용
        if (_rankerData.uid == DataManager.userInfo.uid)
            m_element.imgPanel.color = Color.gray9;
        else
            m_element.imgPanel.color = Color.white;

        // 아이콘
        m_element.profile.SetProfileData(_rankerData.indexProfile, _rankerData.skin);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Image imgPanel;

        public TextMeshProUGUI txtRank;
        public TextMeshProUGUI txtPrevRank;

        public ProfileIconCompoent profile;

        public TextMeshProUGUI txtNickname;
        public TextMeshProUGUI txtPower;
        public TextMeshProUGUI txtPoint;

        public Button button;

        public void Initialize(Transform _transform)
        {
            imgPanel = _transform.GetComponent<Image>("Panel");

            txtRank = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_rank");
            txtPrevRank = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_rank/txt_prev");
            txtNickname = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_nickname");
            txtPower = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_power");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_point");

            profile = _transform.GetComponent<ProfileIconCompoent>("Panel/Slot_Profile");
            button = _transform.GetComponent<Button>();
        }
    }
    #endregion VALIDATE

}
