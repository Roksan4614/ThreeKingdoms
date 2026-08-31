using System.Linq;
using TMPro;
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
        m_element.btnReroll.text = "_부여_";
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
            {
                m_element.slots[i].SetTraitsData(_heroData.key, _heroData.traits[i]);
                m_element.slots[i].onCallback_Reroll = () => SetRerollCost(_heroData);
            }
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

        SetRerollCost(_heroData);
    }

    void SetRerollCost(HeroInfoData _heroData)
    {
        int countLock = _heroData.traits == null ?
            0 : _heroData.traits.Count(x => x.isLock == true);

        //리롤 가능한 갯수
        var countReroll = _heroData.countOpenTraits - countLock;

        var cost = 100 * Mathf.Min(3, (_heroData.countOpenTraits - countReroll + 1));
        m_element.txtCost.text = cost.AmountKMBT(_isMBT: true);

        bool isValidReroll = countReroll > 0;
        m_element.cgReroll.alpha = isValidReroll ? 1f : .5f;
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
        public CanvasGroup cgReroll;

        public PopupHeroInfo_Stat_Attribute_Slot[] slots;

        public TextMeshProUGUI txtCost;

        public void Initialize(Transform _transform)
        {
            btnReroll = _transform.GetComponent<ButtonHelper>("btn_reroll");
            cgReroll = btnReroll.transform.GetComponent<CanvasGroup>();
            slots = _transform.GetComponentsInChildren<PopupHeroInfo_Stat_Attribute_Slot>();

            txtCost = _transform.GetComponent<TextMeshProUGUI>("btn_reroll/Amount/Text");
        }
    }
    #endregion VALIDATE

}
