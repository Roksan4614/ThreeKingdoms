using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoopScrollHelper_Horizontal : MonoBehaviour, IValidatable
{
    float m_moveValueX;
    int m_countItem;

    public RectTransform content => m_element.scroll.content;
    public Transform empty => m_element.empty;

    private void Awake()
    {
        float availableWidth = m_element.scroll.viewport.rect.width - m_element.layout.padding.left - m_element.layout.padding.right;
        float spacing = m_element.layout.spacing;
        int visibleCount = Mathf.CeilToInt((availableWidth + spacing) / (m_element.rtBaseItem.rect.width + spacing)) + 2;

        m_element.rtBaseItem.name = "0";
        for (int i = 1; i < visibleCount; i++)
            Instantiate(m_element.rtBaseItem, m_element.scroll.content);

        m_moveValueX = visibleCount * m_element.rtBaseItem.rect.width + (m_element.layout.spacing * visibleCount);
        m_element.layout.enabled = false;
    }

    private void Start()
    {
        ////m_element.scroll.content.ForceRebuildLayout();
        ////m_element.layout.enabled = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_count">데이타가 몇개야??</param>
    /// <param name="_onUpdate">아이템이랑, 데이타 인덱스</param>
    public void Initialize<T>(int _count, UnityAction<T, int> _onUpdate)
    {
        m_curIndex = 0;
        m_countItem = _count;

        // content size 세팅
        var sizeDelta = m_element.scroll.content.sizeDelta;
        sizeDelta.x = m_element.layout.padding.left + m_element.layout.padding.right
            + (_count * m_element.rtBaseItem.rect.width)
            + ((_count - 1) * m_element.layout.spacing);
        m_element.scroll.content.sizeDelta = sizeDelta;

        // 오브젝트 위치 조정
        float itemChunkHeight = m_element.rtBaseItem.sizeDelta.x + m_element.layout.spacing;
        for (int i = 0; i < m_element.scroll.content.childCount; i++)
        {
            var rtItem = (RectTransform)m_element.scroll.content.GetChild(i);
            rtItem.gameObject.SetActive(i < _count);

            if (rtItem.gameObject.activeSelf == true)
            {
                _onUpdate(rtItem.GetComponent<T>(), i);
                var pos = rtItem.anchoredPosition;
                pos.x = -m_element.layout.padding.left - itemChunkHeight * i - (1 - m_element.rtBaseItem.pivot.x) * m_element.rtBaseItem.rect.width;
                rtItem.anchoredPosition = pos;
            }
        }

        m_element.scroll.velocity = m_element.scroll.content.anchoredPosition = Vector2.zero;
        m_element.scroll.onValueChanged.RemoveAllListeners();

        m_element.scroll.onValueChanged.AddListener(_pos =>
        {
            OnValueChanged(_pos, (_indexChild, _indexData) =>
            {
                _onUpdate(m_element.scroll.content.GetChild(_indexChild).GetComponent<T>(), _indexData);
            });
        });

        if (m_element.empty == true)
            m_element.empty.gameObject.SetActive(_count == 0);
    }

    int m_curIndex = 0;
    private void OnValueChanged(Vector2 _pos, UnityAction<int, int> _onUpdate)
    {
        if (_pos.x < -0.001 || _pos.x > 1.001)
            return;

        var content = m_element.scroll.content;

        float itemChunkWidth = m_element.rtBaseItem.sizeDelta.x + m_element.layout.spacing;
        float scrolledDistance = content.anchoredPosition.x - m_element.layout.padding.left;

        var prevIndex = m_curIndex;
        var curIndex = m_curIndex = Mathf.Max(0, (int)(scrolledDistance / itemChunkWidth));

        // 스크롤을 내렸을 때
        int count = prevIndex;
        while (prevIndex < curIndex)
        {
            //맨 위에 있는 걸 아래로 내려줄꺼야
            var first = (RectTransform)content.GetChild(0);
            first.SetAsLastSibling();

            var pos = first.anchoredPosition;
            pos.x -= m_moveValueX;

            first.anchoredPosition = pos;

            if (prevIndex++ + content.childCount >= m_countItem)
                first.gameObject.SetActive(false);
            else
                _onUpdate?.Invoke(content.childCount - 1, count++ + content.childCount);
        }

        // 스크롤을 올렸을 때
        while (prevIndex > curIndex)
        {
            //맨 위에 있는 걸 아래로 내려줄꺼야
            var last = (RectTransform)content.GetChild(content.childCount - 1);
            last.SetAsFirstSibling();

            var pos = last.anchoredPosition;
            pos.x += m_moveValueX;
            last.anchoredPosition = pos;

            last.gameObject.SetActive(true);

            _onUpdate?.Invoke(0, --count);
            curIndex++;
        }
    }

    public void MoveToIndex(int _index, bool _isTween)
    {
        var pos = content.anchoredPosition;

        _index -= (int)(m_element.scroll.content.childCount * 0.5f);
        if (_index < 0)
            pos.x = 0;
        else
        {
            if (_index > m_countItem - m_element.scroll.content.childCount + 2)
                _index = m_countItem - m_element.scroll.content.childCount + 2;

            var value = m_element.layout.padding.left + 
                m_element.rtBaseItem.rect.width * (_index + 1) + m_element.layout.spacing * (_index);
            pos.x = Mathf.Min(m_element.scroll.content.rect.width - m_element.scroll.viewport.rect.width, value);
        }

        if (_isTween)
            content.DOAnchorPosX(pos.x, 0.1f).OnComplete(() => m_element.scroll.velocity = Vector2.zero);
        else
            content.anchoredPosition = pos;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ScrollRect scroll;
        public RectTransform rtBaseItem;
        public HorizontalOrVerticalLayoutGroup layout;

        public Transform empty;

        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<ScrollRect>();
            if (scroll.content.childCount > 0)
                rtBaseItem = (RectTransform)scroll.content.GetChild(0);

            layout = scroll.content.GetComponent<HorizontalOrVerticalLayoutGroup>();

            empty = scroll.viewport.Find("Empty");
        }
    }
    #endregion VALIDATE

}
