using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Data_Castle;

public class LobbyScreen_Castle : LobbyScreen_Base
{
    Dictionary<CastleObjectType, Vector2> m_dbPosObject = new();

    LobbyScreen_Castle_Popup_Setting m_popupSetting;
    LobbyScreen_Castle_Popup_Menu m_popupMenu;

    protected override void Awake()
    {
        var panel = transform.Find("Panel");
        m_btnBack.transform.SetParent(panel);
        m_txtTitle.transform.SetParent(panel);

        base.Awake();

        var popup = transform.Find("Popup");
        {
            m_popupMenu = popup.GetComponent<LobbyScreen_Castle_Popup_Menu>("Popup_Menu");
            m_popupSetting = popup.GetComponent<LobbyScreen_Castle_Popup_Setting>("Popup_Setting");
            for (int i = 0; i < popup.childCount; i++)
                popup.GetChild(i).gameObject.SetActive(false);
        }

        for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
        {
            var type = i;
            m_dbPosObject.Add(i, m_element.objectPosition[(int)i]);
            var btn = m_element.btnObject[(int)i];
            btn.onClick.AddListener(()
                => OnButtonAsync_Object(type).Forget());

            var parent = m_element.panelMap.Find(type.ToString());
            btn.transform.SetParent(parent);
            btn.transform.SetAsLastSibling();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        m_element.scroll.content.anchoredPosition = Vector2.zero;
    }

    bool m_isSwitchEscape = true;

    protected override bool IsCloseScreen()
    {
        if (m_isSwitchEscape == false)
            return false;

        if (m_popupMenu.gameObject.activeSelf == true)
        {
            m_popupMenu.Close();
            return false;
        }

        if (m_popupSetting.CloseEscape() == false)
            return false;

        return true;
    }

    async UniTask OnButtonAsync_Object(CastleObjectType _objectType)
    {
        int idx = (int)_objectType;
        for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
            m_element.btnObject[(int)i].gameObject.SetActive(i == _objectType);

        m_popupMenu.Open(m_element.btnObject[idx].transform, _objectType);

        await UniTask.WaitUntil(() => m_popupMenu.statusType != StatusType.Wait);

        // 包府
        if (m_popupMenu.statusType == StatusType.Success || m_popupMenu.statusType == StatusType.Failed)
        {
            m_element.scroll.enabled = false;

            var rtObject = (RectTransform)m_element.panelMap.Find(_objectType.ToString());

            m_element.panelMap.DOScale(Vector3.one * (_objectType == CastleObjectType.Gate ? 1.5f : 2), 0.1f);
            m_element.scroll.content.DOAnchorPos(m_element.objectPosition[idx], 0.1f);

            var objButton = m_element.btnObject[idx].gameObject;
            objButton.SetActive(false);

            bool isInfo = m_popupMenu.statusType == StatusType.Success;
            await m_popupSetting.OpenAsync(isInfo, _objectType, m_cts.Token);

            m_element.panelMap.DOScale(Vector3.one, 0.1f);
            await m_element.scroll.content.DOAnchorPos(Vector3.zero, 0.1f).AsyncWaitForCompletion();

            objButton.SetActive(true);
            m_element.scroll.enabled = true;
        }
        // 漂荐
        else if (m_popupMenu.statusType == StatusType.Cancel)
        {
            m_isSwitchEscape = false;

            //青惑老版快
            switch (_objectType)
            {
                case CastleObjectType.Merchant:
                    await OpenPopup_Merchant();
                    break;
                case CastleObjectType.Office:
                    await OpenPopup_Office();
                    break;
            }

            m_isSwitchEscape = true;
        }

        for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
            m_element.btnObject[(int)i].gameObject.SetActive(true);
    }

    async UniTask OpenPopup_Merchant()
    {
        await UniTask.Yield();
    }
    async UniTask OpenPopup_Office()
    {
        await PopupManager.instance.OpenPopupAndWait(PopupType.Castle_Mission);
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public List<Vector2> objectPosition;

        public ScrollRect scroll;

        public RectTransform panelMap;
        public Transform pButtons;
        public ButtonHelper[] btnObject;
        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");

            panelMap = scroll.content.GetComponent<RectTransform>("Map/Panel");
            pButtons = scroll.content.Find("Buttons");
            btnObject = pButtons.GetComponentsInChildren<ButtonHelper>();
        }
    }
    #endregion VALIDATE
}