using System.Linq;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rev9.Pass
{
    public class PopupPass_Mission : PopupPass_ContentBase
    {
        public UnityAction<PopupPass_Mission_Group_Slot> actionComplete { get; set; }

        private void Start()
        {
            m_element.groupDaily.SetTitle(LimitResetType.Daily);
            m_element.groupSeason.SetTitle(LimitResetType.Season);

            InitializeScroll();
        }

        void InitializeScroll()
        {
            var db = DataManager.pass.quests.GroupBy(x => x.data.type).ToDictionary(x => x.Key, x => x.ToList());

            foreach (var data in db)
            {
                var group = data.Key == QuestDateType.Daily ? m_element.groupDaily : m_element.groupSeason;
                group.actionComplete = actionComplete;
                group.SetQuestData(data.Value);
            }

            m_element.scroll.content.ForceRebuildLayout();
        }

        #region VALIDATE
        public override void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public ScrollRect scroll;

            public PopupPass_Mission_Group groupDaily;
            public PopupPass_Mission_Group groupSeason;

            public void Initialize(Transform _transform)
            {
                scroll = _transform.GetComponent<ScrollRect>();

                groupDaily = scroll.content.GetComponent<PopupPass_Mission_Group>("Daily");
                groupSeason = scroll.content.GetComponent<PopupPass_Mission_Group>("Season");
            }
        }
        #endregion VALIDATE

    }
}