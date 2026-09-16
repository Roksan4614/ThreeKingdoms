using UnityEngine;
using UnityEngine.Events;

namespace Rev9.Pass
{
    public class PopupPass_Mission : PopupPass_ContentBase
    {
        public UnityAction<PopupPass_Mission_Group_Slot> actionComplete { get; set; }
    }
}