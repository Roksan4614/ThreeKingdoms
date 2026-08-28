using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo_Stat_Attribute : MonoBehaviour, IValidatable
{

    public Button.ButtonClickedEvent onClickReroll
        => m_element.btnReroll.onClick;

    public bool interactable { set => m_element.btnReroll.interactable = value; }
    public bool isActive => gameObject.activeSelf;

    private void Awake()
    {

        //setlocalization
        m_element.btnReroll.text = "새로_부여";
    }

    public void SetActive(bool _isActive, HeroInfoData _heroData)
    {
        gameObject.SetActive(_isActive);

        if (_isActive == false)
            return;

        int i = 0;
        if (_heroData.traits != null)
        {
            for (; i < _heroData.traits.Count; i++)
                m_element.slots[i].SetTraitsData(_heroData.traits[i]);
        }

        for (; i < _heroData.countOpenTraits; i++)
        {
            var gradeType = GradeType.General + i;
            m_element.slots[i].SetNotOpen(gradeType);
        }

        for (; i < m_element.slots.Length; i++)
        {
            var gradeType = GradeType.General + i;
            m_element.slots[i].SetNotReady(gradeType);
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
        public ButtonHelper btnReroll;
        public PopupHeroInfo_Stat_Attribute_Slot[] slots;

        public void Initialize(Transform _transform)
        {
            btnReroll = _transform.GetComponent<ButtonHelper>("btn_reroll");
            slots = _transform.GetComponentsInChildren<PopupHeroInfo_Stat_Attribute_Slot>();
        }
    }
    #endregion VALIDATE

}
