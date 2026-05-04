using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Castle : LobbyScreen_Base
{
    Dictionary<CastleObjectType, Vector2> m_dbPosObject = new();

    LobbyScreen_Castle_Popup_Setting m_popupSetting;
    LobbyScreen_Castle_Popup_Menu m_popupMenu;

    Vector2 m_posPrevMap;

    protected override void Awake()
    {
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

            var parent = m_element.pMap.Find($"Panel/{type.ToString()}");
            btn.transform.SetParent(parent);
            btn.transform.SetAsLastSibling();
        }

        m_posPrevMap = new Vector2(0, 100);

    }

    protected override void OnEnable()
    {
        base.OnEnable();
        m_element.scroll.content.anchoredPosition = Vector2.zero;
        m_element.pMap.anchoredPosition = m_posPrevMap;
    }

    async UniTask OnButtonAsync_Object(CastleObjectType _objectType)
    {
        for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
            m_element.btnObject[(int)i].gameObject.SetActive(i == _objectType);

        m_popupMenu.Open(m_element.btnObject[(int)_objectType].transform, _objectType);

        await UniTask.WaitUntil(() => m_popupMenu.statusType != StatusType.Wait);

        if (m_popupMenu.statusType == StatusType.Success)
        {
            m_element.scroll.enabled = false;

            var rtObject = (RectTransform)m_element.pMap.Find($"Panel/{_objectType.ToString()}");
            var targetPos = rtObject.anchoredPosition * -1;

            m_element.pMap.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.1f);
            m_element.pMap.DOAnchorPos(targetPos, 0.1f);

            var objButton = m_element.btnObject[(int)_objectType].gameObject;
            objButton.SetActive(false);

            await m_popupSetting.OpenAsync(_objectType, m_cts.Token);

            m_element.pMap.DOScale(Vector3.one, 0.1f);
            await m_element.pMap.DOAnchorPos(m_posPrevMap, 0.1f).AsyncWaitForCompletion();

            objButton.SetActive(true);
            m_element.scroll.enabled = true;
        }
        else if (m_popupMenu.statusType == StatusType.Valid)
        {
            //행상일경우
            switch (_objectType)
            {
                case CastleObjectType.Merchant:
                    await OpenPopup_Merchant();
                    break;
                case CastleObjectType.Office:
                    await OpenPopup_Office();
                    break;
            }
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
        await UniTask.Yield();
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

        public RectTransform pMap;
        public Transform pButtons;
        public ButtonHelper[] btnObject;
        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");

            pMap = scroll.content.GetComponent<RectTransform>("Map");
            pButtons = scroll.content.Find("Buttons");
            btnObject = pButtons.GetComponentsInChildren<ButtonHelper>();
        }
    }
    #endregion VALIDATE
}

public enum CastleObjectType
{
    NONE = -1,

    Palace,
    Market,
    Farm,
    Office,
    Merchant,
    Gate,
    //Wall,
    MAX
}