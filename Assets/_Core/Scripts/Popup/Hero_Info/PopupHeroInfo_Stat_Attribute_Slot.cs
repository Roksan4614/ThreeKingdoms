using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo_Stat_Attribute_Slot : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        m_element.objPanel.SetActive(false);
        m_element.objUnOpen.SetActive(true);
    }

    public void SetTraitsData(HeroTraitsData _traitData)
    {
        if (m_element.objPanel.activeSelf == false)
        {
            m_element.objPanel.SetActive(true);
            m_element.objUnOpen.SetActive(false);
        }

        var grade = TableManager.stringTable.GetString($"GRADE_QUALITY_{_traitData.traitsValueData.grade.ToString().ToUpper()}");
        var msg = TableManager.stringTraits.GetString($"{_traitData.traitsValueData.key.ToUpper()}_TITLE");
        m_element.txtName.text = $"[{grade}] {msg}";

        if (TableManager.traits.Get(_traitData.type).isLegend == true)
            m_element.txtValue.text = "";
        else
            m_element.txtValue.text = _traitData.stringValue;
    }

    public void SetNotOpen(GradeType _gradeType)
    {
        var msg = "특성_새로_부여_가능";
        m_element.txtUnOpen.text = msg;
    }

    public void SetNotReady(GradeType _gradeType)
    {
        var grade = TableManager.stringTable.GetString($"GRADE_{_gradeType.ToString().ToUpper()}");
        m_element.txtUnOpen.text = $"<color=#999999>[{grade}]_등급_달성_시_해제";
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

        public Button btnLock;

        public void Initialize(Transform _transform)
        {
            txtName = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_name");
            txtValue = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_value");
            btnLock = _transform.GetComponent<Button>("Panel/btn_lock");

            txtUnOpen = _transform.GetComponent<TextMeshProUGUI>("UnOpen/Text");
        }

        public GameObject objPanel => txtName.transform.parent.gameObject;
        public GameObject objUnOpen => txtUnOpen.transform.parent.gameObject;
    }
    #endregion VALIDATE

}
