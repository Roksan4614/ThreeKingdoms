using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupHeroInfo_Stat_Attribute_Slot : MonoBehaviour, IValidatable
{
    public UnityAction onCallback_Reroll { get; set; }

    public void SetTraitsData(string _keyHero, HeroTraitsData _traitData)
    {
        SetActivePanel(true);

        var grade = TableManager.stringTable.GetString($"GRADE_QUALITY_{_traitData.traitsValueData.grade.ToString().ToUpper()}");
        var msg = TableManager.stringTraits.GetString($"{_traitData.traitsValueData.key.ToUpper()}_TITLE");
        m_element.txtName.text = $"[{grade}] {msg}";

        if (TableManager.traits.Get(_traitData.type).isLegend == true)
            m_element.txtValue.text = "";
        else
            m_element.txtValue.text = _traitData.stringValue;

        // LOCK
        {
            m_element.btnLock.onClick.RemoveAllListeners();
            m_element.btnLock.onClick.AddListener(() => OnButtonAsync_Lock(_keyHero, _traitData).Forget());

            m_element.objLock.SetActive(_traitData.isLock);
            m_element.txtName.color = m_element.txtValue.color = _traitData.isLock ? Color.white : Color.black;
        }
    }

    async UniTask OnButtonAsync_Lock(string _keyHero, HeroTraitsData _traitData)
    {
        bool result = await DataManager.userInfo.API_TraitsLock(_keyHero, _traitData.index);

        if (result == true)
        {
            _traitData.isLock = !_traitData.isLock;

            m_element.objLock.SetActive(_traitData.isLock);
            m_element.txtName.color = m_element.txtValue.color = _traitData.isLock ? Color.white : Color.black;

            onCallback_Reroll?.Invoke();
        }
    }

    public void SetNotOpen(GradeType _gradeType)
    {
        SetActivePanel(false);

        var msg = "특성_새로_부여_가능";
        m_element.txtUnOpen.text = msg;
    }

    public void SetNotReady(GradeType _gradeType)
    {
        SetActivePanel(false);

        var grade = TableManager.stringTable.GetString($"GRADE_{_gradeType.ToString().ToUpper()}");
        m_element.txtUnOpen.text = $"<color=#999999>[{grade}]_등급_달성_시_해제";
    }

    void SetActivePanel(bool _isActive)
    {
        if (m_element.objPanel.activeSelf != _isActive)
        {
            m_element.objPanel.SetActive(_isActive);
            m_element.objUnOpen.SetActive(_isActive == false);
        }

    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtValue;
        public TextMeshProUGUI txtUnOpen;

        public GameObject objLock;
        public Button btnLock;

        public void Initialize(Transform _transform)
        {
            txtName = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_name");
            txtValue = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_value");
            objLock = _transform.Find("Panel/BG_Lock").gameObject;
            btnLock = _transform.GetComponent<Button>("Panel/btn_lock");

            txtUnOpen = _transform.GetComponent<TextMeshProUGUI>("UnOpen/Text");
        }

        public GameObject objPanel => txtName.transform.parent.gameObject;
        public GameObject objUnOpen => txtUnOpen.transform.parent.gameObject;
    }
    #endregion VALIDATE

}
