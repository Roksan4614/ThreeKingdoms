using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PopupTournament_Batch_Hero : LobbyScreen_Hero_Hero
{
    bool m_isAttackType;

    struct ButtonActionData
    {
        public ButtonHelper btnSlot;
        public ButtonHelper btnAction;
    }

    ButtonActionData[] m_batch;

    protected override void Awake()
    {
        base.Awake();

        var pAction = transform.Find("Batch_Action");
        m_batch = new ButtonActionData[9];
        for (int i = 0; i < m_batch.Length; i++)
        {
            var idx = i;
            m_batch[i].btnSlot = (i == pAction.childCount ? Instantiate(pAction.GetChild(0), pAction) : pAction.GetChild(i)).GetComponent<ButtonHelper>();

            var slot = m_batch[i].btnSlot;
            slot.name = "Slot_" + i;
            m_batch[i].btnAction = slot.transform.GetComponent<ButtonHelper>("btn_action");
            m_batch[i].btnAction.gameObject.SetActive(false);
            slot.imgChild.gameObject.SetActive(false);

            slot.funcDown = () => IngameLog.Add("FUNC DOWN: " + slot.name);
            slot.funcUp = () => IngameLog.Add("FUNC UP: " + slot.name);
            slot.funcEnter = () => m_batch[idx].btnSlot.imgChild.gameObject.SetActive(true);
            slot.funcExit = () => m_batch[idx].btnSlot.imgChild.gameObject.SetActive(false);
        }
    }

    protected override void SetFilterSize()
    {
        var offsetMax = m_popupFilter.rtPanel.offsetMax;
        offsetMax.y = -810;
        m_popupFilter.SetFilterSize(offsetMax);
    }

    protected override void Start()
    {
        InstantiateList();
        m_isStarted = true;

        m_isAttackType = false;
        OnButton_Type(true);

        m_elementTournament.btnAttack.onClick.AddListener(() => OnButton_Type(true));
        m_elementTournament.btnDefence.onClick.AddListener(() => OnButton_Type(false));
    }

    protected override void OnEnable()
    {
        if (m_isStarted == true)
            OnButton_Type(true);
    }

    void OnButton_Type(bool _isAttackType)
    {
        if (m_isAttackType == _isAttackType)
            return;

        m_isAttackType = _isAttackType;

        m_myHero.Clear();
        m_myHero.AddRange(TournamentWorker.instance.GetBatchData(m_isAttackType).heroInfo);

        SetLayout_List();
    }


    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_elementTournament.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    ElementData_Tournament m_elementTournament;

    [System.Serializable]
    struct ElementData_Tournament
    {
        public ButtonHelper btnAttack;
        public ButtonHelper btnDefence;

        public void Initialize(Transform _transform)
        {
            btnAttack = _transform.GetComponent<ButtonHelper>("Tab_Type/btn_attack");
            btnDefence = _transform.GetComponent<ButtonHelper>("Tab_Type/btn_defence");
        }
    }
}
