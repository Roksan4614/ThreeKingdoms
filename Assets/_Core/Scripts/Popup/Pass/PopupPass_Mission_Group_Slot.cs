using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rev9.Pass
{
    public class PopupPass_Mission_Group_Slot : MonoBehaviour, IValidatable
    {
        public PassQuestData data { get; private set; }
        public UnityAction<PopupPass_Mission_Group_Slot> actionComplete { get; set; }

        private void Awake()
        {
            //m_element.btnConfirm.text = TableManager.stringTable.GetString("BUTTON_RECEIVE");
            //m_element.btnConfirm.onClick.AddListener(()
            //    => actionComplete(this));
            transform.GetComponent<Button>().onClick.AddListener(() => actionComplete(this));
            m_element.txtBadge.text = TableManager.stringTable.GetString("UI_PASS_QUEST_PAID");
        }

        public void SetQuestData(PassQuestData _data)
        {
            data = _data;
            var tableData = _data.tableData;

            m_element.txtTitle.text = tableData.name;
            m_element.txtCount.text = $"XP+{tableData.exp}";

            var heroName = tableData.resultValueName;
            if (heroName.IsActive())
                heroName = KoreanHelper.AppendJosa(heroName
                    , tableData.key == ThreeKingdoms.Shared.Enums.QuestType.OfficeDispatch ? KoreanHelper.JosaType.EulLeul : KoreanHelper.JosaType.EuroroRo
                    , "<size=120%>[{0}]</size>");
            m_element.txtDesc.text = tableData.GetDesc(heroName);
            m_element.txtDesc.text += $"\n<color=#555555>({tableData.count}/{TableManager.passQuest.GetCount(tableData)})";

            m_element.badge.SetActive(_data.isPaid && (DataManager.pass.isPaid == false));

            //FFFFBA
            if (_data.isComplete)
            {
                ColorUtility.TryParseHtmlString("#C3C3C3", out Color outClr);
                m_element.imgSlot.color = outClr;
            }
            else
                m_element.imgSlot.color = Color.white;

            //if (QuestWorker.instance.HasNavigation(_data.data.key) == false)
            //{
            //    if (_data.isComplete == false)
            //    {
            //        m_element.btnConfirm.gameObject.SetActive(false);
            //        return;
            //    }
            //}

            //m_element.btnConfirm.gameObject.SetActive(true);
            //m_element.btnConfirm.text = TableManager.stringTable.GetString($"BUTTON_{(_data.isComplete ? "RECEIVE" : "NAVIGATION")}");

            //if (m_element.btnConfirm.isDrawSelect != _data.isComplete)
            //    m_element.btnConfirm.SetDrawSelect(_data.isComplete);
        }

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public TextMeshProUGUI txtTitle;
            public TextMeshProUGUI txtCount;
            public TextMeshProUGUI txtDesc;
            public TextMeshProUGUI txtBadge;

            public Image imgSlot;

            //public ButtonHelper btnConfirm;

            public void Initialize(Transform _transform)
            {
                imgSlot = _transform.GetComponent<Image>();
                txtTitle = _transform.GetComponent<TextMeshProUGUI>("Title/Text");
                txtCount = _transform.GetComponent<TextMeshProUGUI>("Title/txt_count");
                txtDesc = _transform.GetComponent<TextMeshProUGUI>("txt_desc");
                txtBadge = _transform.GetComponent<TextMeshProUGUI>("Badge/Badge/Text");
                //btnConfirm = _transform.GetComponent<ButtonHelper>("btn_confirm");
            }

            public GameObject badge => txtBadge.transform.parent.parent.gameObject;
        }
        #endregion VALIDATE

    }

}