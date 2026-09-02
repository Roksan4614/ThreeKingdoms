using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Rendering.DebugUI;

namespace Rev9.Inventory
{
    public class PopupInventoryComponent : BasePopupComponent
    {
        PopupInventoryComponent() : base(PopupType.Inventory) { }

        public RectTransform panel => m_element.panel;

        Dictionary<ItemCategoryType, List<ItemType>> m_group = new();

        protected override void Awake()
        {
            base.Awake();

            m_group.Add(ItemCategoryType.Currency, new List<ItemType>() {
                ItemType.time_stone,
            });

            m_group.Add(ItemCategoryType.Point, new List<ItemType>() {
                ItemType.tournament_point,
                ItemType.raid_point,
            });

            m_group.Add(ItemCategoryType.Ticket, new List<ItemType>() {
                ItemType.tournament_ticket,
            });

            m_group.Add(ItemCategoryType.Soul_Stone, new List<ItemType>() {
                ItemType.public_soul_stone,
                ItemType.class_soul_stone,
            });
        }

        private void Start()
        {
            int i = 0;
            foreach (var g in m_group)
            {
                var group = (i == 0 ? panel.GetChild(0) : Instantiate(panel.GetChild(0), panel)).GetComponent<PopupInventory_Group>();
                group.InitializeGroup(g.Key, g.Value);
                group.onClick = OnButton_Item;
                group.actionTrigger = (_slot, _isEnter) => OnTriggerAsync(_slot, _isEnter).Forget();
                i++;
            }

            panel.ForceRebuildLayout();
            m_element.rtTooltip.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            m_ctsTooltip = m_ctsTooltip.ReleaseCTS();
        }

        void OnButton_Item(ItemData _itemData)
        {
            //IngameLog.Add($"OnButton: {_itemData.nameValue}");
        }



        CancellationTokenSource m_ctsTooltip;
        int m_idxTrigger;
        async UniTask OnTriggerAsync(PopupInventory_Group_Slot _slot, bool _isEnter)
        {
            if (_isEnter == false)
            {
                if (m_idxTrigger == _slot.idxTrigger)
                {
                    m_idxTrigger = -1;
                    m_element.rtTooltip.gameObject.SetActive(false);
                    m_ctsTooltip = m_ctsTooltip.ReleaseCTS();
                }
            }
            else
            {
                m_ctsTooltip = m_ctsTooltip.ReleaseCTS(true);
                var token = m_ctsTooltip.Token;

                m_idxTrigger = _slot.idxTrigger;
                m_element.txtTooltip.text = _slot.itemData.nameValue;

                await UniTask.WaitForSeconds(.2f, cancellationToken: token);

                m_element.rtTooltip.SetParent(_slot.icon);
                m_element.rtTooltip.anchoredPosition = Vector2.zero;

                m_element.rtTooltip.gameObject.SetActive(true);
            }
        }

        public override void Close()
        {
            SetActivePunchAsync(false).Forget();
        }

        public async UniTask SetActivePunchAsync(bool _isActive)
        {
            if (_isActive)
                gameObject.SetActive(true);

            float targetScale = _isActive ? 1 : 0.8f;
            if (_isActive)
            {
                var scale = panel.localScale;
                scale.y = .8f;
                panel.localScale = scale;
            }
            var duration = 0.05f;

            panel.DOKill();
            await panel.DOScaleY(targetScale, duration).SetEase(_isActive ? Ease.OutBack : Ease.InBack).AsyncWaitForCompletion();

            if (_isActive == false)
                gameObject.SetActive(false);
        }

        #region VALIDATE
        public override void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public RectTransform panel;
            public TextMeshProUGUI txtTooltip;

            public void Initialize(Transform _transform)
            {
                panel = (RectTransform)_transform.Find("Panel");
                txtTooltip = _transform.GetComponent<TextMeshProUGUI>("Tooltip/Text");
            }

            public RectTransform rtTooltip => (RectTransform)txtTooltip.transform.parent;
        }
        #endregion VALIDATE

    }
}