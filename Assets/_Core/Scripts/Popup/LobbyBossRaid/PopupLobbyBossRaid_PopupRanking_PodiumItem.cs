using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupLobbyBossRaid_PopupRanking_PodiumItem : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        while (m_element.pHero.childCount > 0)
            DestroyImmediate(m_element.pHero.GetChild(0).gameObject);
    }

    public async UniTask SetRankerInfoAsync(Data_BossRaid.BossRaidRankerUserData _rankerData, UnityAction<Data_BossRaid.BossRaidRankerUserData> _callback)
    {
        m_element.btnUserInfo.onClick.RemoveAllListeners();
        m_element.btnUserInfo.onClick.AddListener(() => _callback(_rankerData));

        m_element.txtName.text = _rankerData.nickname;
        m_element.txtPoint.text = $"{_rankerData.point:#,0}p";

        // 캐릭터 생성
        {
            bool isFinded = false;
            for (int i = 0; i < m_element.pHero.childCount; i++)
            {
                var obj = m_element.pHero.GetChild(i).gameObject;
                obj.SetActive(obj.name.Contains(_rankerData.skin));

                if (isFinded == false && obj.activeSelf == true)
                    isFinded = true;
            }

            if (isFinded == false)
            {
                var objHero = Instantiate(await AddressableManager.instance.GetHeroCharacterAsync(_rankerData.skin), m_element.pHero);
                objHero.transform.localPosition = Vector3.zero;
            }
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Transform pHero;

        public Button btnUserInfo;
        public TextPanelHelper txtName;
        public TextMeshProUGUI txtPoint;

        public void Initialize(Transform _transform)
        {
            pHero = _transform.Find("Panel/Hero");

            btnUserInfo = _transform.GetComponent<Button>("Panel/btn_userInfo");
            txtName = _transform.GetComponent<TextPanelHelper>("Panel/Info/TextPanelHelper");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/Info/txt_point");
        }
    }
    #endregion VALIDATE

}
