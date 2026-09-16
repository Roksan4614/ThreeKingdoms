using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Pass
{
    public class PopupPass_Reward : PopupPass_ContentBase
    {
        float m_paddigTop;
        float m_heightSlot;

        private void Start()
        {
            var db = TableManager.passReward.list;

            var content = m_element.scroll.content;

            m_element.scroll.Initialize<PopupPass_Reward_Slot>(db.Count - 1, (_slot, _idxData) =>
            {
                _slot.SetRewardData(db[_idxData]);
                if (_slot.actionReward == null)
                {
                    _slot.actionReward = (_slot, _isPaid) => OnButtonAsync_Reward(_slot, _isPaid).Forget();

                    if (m_heightSlot == 0)
                        m_heightSlot = _slot.rt.rect.height;
                }

                OnValueChanged();
            });

            SetPositionOpen();
        }

        private void OnEnable()
        {
            if (m_heightSlot == 0)
                return;

            SetPositionOpen();
        }

        void SetPositionOpen()
        {
            Vector2 pos = m_element.scroll.content.anchoredPosition;
            pos.y = m_paddigTop + m_heightSlot * (DataManager.pass.level - 1);

            m_element.scroll.scroll.content.anchoredPosition = pos;
            m_element.scroll.scroll.onValueChanged.Invoke(default);
        }

        int m_stepLevel;
        void OnValueChanged()
        {
            var pos = m_element.scroll.scroll.content.anchoredPosition;
            int level = (int)((pos.y + m_paddigTop + m_heightSlot) / m_heightSlot) + 3;

            var stepLevel = (Mathf.Max(level, DataManager.pass.level) + 10) / 10 * 10;
            if (m_stepLevel != stepLevel)
            {
                m_stepLevel = stepLevel;
                m_element.slotStep.SetRewardData(TableManager.passReward.GetRewardData(stepLevel));
            }
        }

        async UniTask OnButtonAsync_Reward(PopupPass_Reward_Slot _slot, bool _isPaid)
        {
            IngameLog.Add($"OnButtonAsync_Reward: {_slot.rewardData.level}: {_isPaid}");
        }

        #region VALIDATE
        public override void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public LoopScrollHelper scroll;
            public PopupPass_Reward_Slot slotStep;

            public void Initialize(Transform _transform)
            {
                scroll = _transform.GetComponent<LoopScrollHelper>();
                slotStep = scroll.transform.GetComponent<PopupPass_Reward_Slot>("Step");
            }
        }
        #endregion VALIDATE

    }
}