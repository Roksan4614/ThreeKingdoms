using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyScreen_Summon_Package : MonoBehaviour, IValidatable, IEndDragHandler
{
    ScrollRect scroll => m_element.scroll;
    Dictionary<RegionType, Button> m_dbButton;

    RegionType m_curRegion = RegionType.NONE;

    bool m_isPush;

    private void Awake()
    {
        m_dbButton = m_element.buttons.ToDictionary(x =>
        {
            if (Enum.TryParse(x.name, out RegionType region) == false)
                return RegionType.NONE;
            return region;
        }, x => x);

        m_element.scroll.onValueChanged.AddListener(_pos => m_isPush = true);

        SetButtonSort();
    }

    private void Update()
    {
        if (m_tween != null)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            MoveScroll(true, .2f);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            MoveScroll(false, .2f);
    }

    Tween m_tween;
    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_isPush == true)
        {
            bool isChanged = Math.Abs(m_element.scroll.content.anchoredPosition.x)
                > m_element.scroll.viewport.rect.width * .1f;

            if (isChanged)
                MoveScroll(m_element.scroll.content.anchoredPosition.x < 0);
            else
                m_element.scroll.content.DOAnchorPosX(0, 0.1f);

            m_element.scroll.velocity = Vector2.zero;
        }

        m_isPush = false;
    }

    void MoveScroll(bool _isLeft, float _speed = 0.1f)
    {
        m_element.scroll.enabled = false;

        var pos = m_element.scroll.content.anchoredPosition;

        pos.x += _isLeft ? m_element.moveValue : -m_element.moveValue;

        m_element.scroll.content.anchoredPosition = pos;

        m_tween = m_element.scroll.content.DOAnchorPosX(0, _speed);
        m_tween.OnComplete(() =>
        {
            m_tween = null;
            m_element.scroll.enabled = true;
        });

        m_curRegion = _isLeft ? nextRegion : prevRegion;

        SetButtonSort();
    }

    void SetButtonSort()
    {
        foreach (var b in m_dbButton)
        {
            var rt = (RectTransform)b.Value.transform;
            if (b.Key == prevRegion)
                rt.anchoredPosition = m_element.posButton[0];
            else if (b.Key == m_curRegion)
                rt.anchoredPosition = m_element.posButton[1];
            else if (b.Key == nextRegion)
                rt.anchoredPosition = m_element.posButton[2];
            else
            {
                b.Value.gameObject.SetActive(false);
                continue;
            }
            b.Value.gameObject.SetActive(true);
        }
    }

    public RegionType prevRegion => m_curRegion == RegionType.NONE ? RegionType.Wu : m_curRegion - 1;
    public RegionType nextRegion => m_curRegion == RegionType.Wu ? RegionType.NONE : m_curRegion + 1;

    public void SetActive(bool _isActive)
    {
        gameObject.SetActive(_isActive);

        if (_isActive)
            scroll.content.anchoredPosition = Vector2.zero;
    }

    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        [SerializeField] ScrollRect m_scroll;
        public ScrollRect scroll => m_scroll;

        public Button[] buttons;
        public Vector2[] posButton;

        public float moveValue;

        public void Initialize(Transform _transform)
        {
            m_scroll = _transform.GetComponent<ScrollRect>();

            // 전체 위촉오 순으로 넣어주기 위함
            buttons = m_scroll.content.GetComponentsInChildren<Button>(true);
            posButton = buttons.Where(x => x.gameObject.activeSelf == true).Select(x => ((RectTransform)x.transform).anchoredPosition).ToArray();

            buttons = buttons.ToDictionary(x =>
            {
                if (Enum.TryParse(x.name, out RegionType region) == false)
                    return RegionType.NONE;
                return region;
            }, x => x).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value).Values.ToArray();

            moveValue = ((RectTransform)buttons[0].transform).rect.width +
                m_scroll.content.GetComponent<HorizontalLayoutGroup>().spacing;
        }
    }
}
