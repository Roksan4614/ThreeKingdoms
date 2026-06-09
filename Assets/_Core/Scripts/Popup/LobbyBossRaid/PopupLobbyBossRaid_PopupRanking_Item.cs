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

        m_element.txtNickname.text = _rankerData.nickname;
        m_element.txtRank.text = _rankerData.rank.ToString();

        var diff = _rankerData.prevRank - _rankerData.rank;
        m_element.txtPrevRank.text = diff == 0 ? "(-)" : $"<color=#{(diff > 0 ? Palette.htmlString_Up : Palette.htmlString_Down)}>({(diff > 0 ? "+" : "")}{diff})";

        m_element.txtPower.text = $"cp{_rankerData.power:#,0}";

        if (_tabType == PopupLobbyBossRaid_PopupRanking.TabType.Point)
        {
            m_element.txtPoint.text = $"{_rankerData.point:#,0}p";
        }
        else
        {
            m_element.txtPoint.text = $"{_rankerData.point:#,0}";
        }

        // ¾ÆÀÌÄÜ
        if (_rankerData.indexProfile > 0)
            m_element.profile.SetProfileData(_rankerData.indexProfile);
        else
            m_element.profile.SetProfileDataAsync(_rankerData.skin).Forget();
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtRank;
        public TextMeshProUGUI txtPrevRank;

        public ProfileIconCompoent profile;

        public TextMeshProUGUI txtNickname;
        public TextMeshProUGUI txtPower;
        public TextMeshProUGUI txtPoint;

        public Button button;

        public void Initialize(Transform _transform)
        {
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
