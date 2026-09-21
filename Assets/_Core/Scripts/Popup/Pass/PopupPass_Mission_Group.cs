using System.Collections.Generic;
using ThreeKingdoms.Shared.Enums;
using UnityEngine;
using UnityEngine.Events;

namespace Rev9.Pass
{
    public class PopupPass_Mission_Group : MonoBehaviour
    {
        public UnityAction<PopupPass_Mission_Group_Slot> actionComplete { get; set; }

        public void SetTitle(LimitResetType _resetType)
            => transform.SetTextTable("txt_title", TableManager.questString.GetString("CATEGORY_NAME_" + _resetType.ToString().ToUpper()));

        public void SetQuestData(List<PassQuestData> _data)
        {
            int i = 0;
            _data = _data.SortBy(x => x.isPaid ? 1 : -1);
            for (; i < _data.Count; i++)
            {
                var slot = (i == transform.childCount - 1 ? Instantiate(transform.GetChild(1), transform) : transform.GetChild(i + 1))
                    .GetComponent<PopupPass_Mission_Group_Slot>();

                slot.actionComplete = actionComplete;
                slot.SetQuestData(_data[i]);
            }

            for (; i < transform.childCount - 1; i++)
                transform.GetChild(i + 1).gameObject.SetActive(false);

            transform.ForceRebuildLayout();
        }
    }
}