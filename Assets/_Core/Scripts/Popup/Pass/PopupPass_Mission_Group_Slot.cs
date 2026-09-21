using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Rev9.Pass
{
    public class PopupPass_Mission_Group_Slot : MonoBehaviour, IValidatable
    {
        public PassQuestData data { get; private set; }
        public UnityAction<PopupPass_Mission_Group_Slot> actionComplete { get; set; }

        private void Awake()
        {
            m_element.btnConfirm.text = TableManager.stringTable.GetString("BUTTON_RECEIVE");
            m_element.btnConfirm.onClick.AddListener(()
                => actionComplete(this));
            m_element.txtBadge.text = TableManager.stringTable.GetString("UI_PASS_QUEST_PAID");
        }

        public void SetQuestData(PassQuestData _data)
        {
            data = _data;

            m_element.txtTitle.text = _data.data.name;
            m_element.txtCount.text = $"XP+{_data.data.exp}";

            var heroName = TableManager.stringHero.GetName(CharacterName.GuanYu);
            heroName = KoreanHelper.AppendJosa(heroName
                , _data.data.key == ThreeKingdoms.Shared.Enums.QuestType.OfficeDispatch ? KoreanHelper.JosaType.EulLeul : KoreanHelper.JosaType.EuroroRo
                , "[{0}]");
            m_element.txtDesc.text = _data.data.GetDesc(heroName);

            m_element.badge.SetActive(_data.isPaid && (DataManager.pass.isPaid == false));
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
            public ButtonHelper btnConfirm;

            public void Initialize(Transform _transform)
            {
                txtTitle = _transform.GetComponent<TextMeshProUGUI>("Title/Text");
                txtCount = _transform.GetComponent<TextMeshProUGUI>("Title/txt_count");
                txtDesc = _transform.GetComponent<TextMeshProUGUI>("txt_desc");
                txtBadge = _transform.GetComponent<TextMeshProUGUI>("Badge/Badge/Text");
                btnConfirm = _transform.GetComponent<ButtonHelper>("btn_confirm");
            }

            public GameObject badge => txtBadge.transform.parent.parent.gameObject;
        }
        #endregion VALIDATE

    }

}