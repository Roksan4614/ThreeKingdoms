using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoopScrollHelper : MonoBehaviour, IValidatable
{
    float m_moveValueY;
    int m_countItem;

    private void Awake()
    {
        m_element.scroll.content.GetComponent<ContentSizeFitter>().enabled = false;

        var content = m_element.scroll.content;
        float availableHeight = m_element.scroll.viewport.rect.height - m_element.layout.padding.top - m_element.layout.padding.bottom;
        float spacing = m_element.layout.spacing;
        int visibleCount = Mathf.CeilToInt((availableHeight + spacing) / (m_element.rtBaseItem.rect.height + spacing)) + 2;

        m_element.rtBaseItem.name = "0";
        for (int i = 1; i < visibleCount; i++)
        {
            var rtItem = Instantiate(m_element.rtBaseItem, m_element.scroll.content);
            rtItem.name = i.ToString();

            var pos = rtItem.anchoredPosition;
            pos.y = m_element.layout.padding.top + rtItem.rect.height * i + m_element.layout.spacing * (i - 1);
        }

        m_moveValueY = visibleCount * m_element.rtBaseItem.rect.height + (m_element.layout.spacing * visibleCount);
    }

    private void Start()
    {
        m_element.scroll.content.ForceRebuildLayout();
        m_element.layout.enabled = false;

        //test
        Initialize(40, _idx =>
        {
            var childIndex = _idx - m_curIndex;

            IngameLog.Add($"{childIndex} / {_idx}");

            m_element.scroll.content.GetChild(childIndex).name = _idx.ToString();
        });
    }

    void Initialize(int _count, UnityAction<int> _onUpdate)
    {
        m_countItem = _count;

        var sizeDelta = m_element.scroll.content.sizeDelta;
        sizeDelta.y = m_element.layout.padding.top + m_element.layout.padding.bottom
            + (_count * m_element.rtBaseItem.rect.height)
            + ((_count - 1) * m_element.layout.spacing);
        m_element.scroll.content.sizeDelta = sizeDelta;



        m_element.scroll.onValueChanged.RemoveAllListeners();
        m_element.scroll.onValueChanged.AddListener(_pos =>
        {
            OnValueChanged(_pos, _onUpdate);
        });
    }

    int m_curIndex = 0;
    private void OnValueChanged(Vector2 _pos, UnityAction<int> _onUpdate)
    {
        var content = m_element.scroll.content;

        float itemChunkHeight = m_element.rtBaseItem.sizeDelta.y + m_element.layout.spacing;
        float scrolledDistance = content.anchoredPosition.y - m_element.layout.padding.top;

        var prevIndex = m_curIndex;
        var curIndex = m_curIndex = Mathf.Max(0, (int)(scrolledDistance / itemChunkHeight));

        // 스크롤을 내렸을 때
        while (prevIndex < curIndex)
        {
            //맨 위에 있는 걸 아래로 내려줄꺼야
            var first = (RectTransform)content.GetChild(0);
            first.SetAsLastSibling();

            var pos = first.anchoredPosition;
            pos.y -= m_moveValueY;
            first.anchoredPosition = pos;

            if (curIndex + content.childCount >= m_countItem)
                first.gameObject.SetActive(false);

            curIndex--;

            _onUpdate?.Invoke(curIndex + content.childCount);
        }

        // 스크롤을 올렸을 때
        while (prevIndex > curIndex)
        {
            //맨 위에 있는 걸 아래로 내려줄꺼야
            var last = (RectTransform)content.GetChild(content.childCount - 1);
            last.SetAsFirstSibling();

            var pos = last.anchoredPosition;
            pos.y += m_moveValueY;
            last.anchoredPosition = pos;

            last.gameObject.SetActive(true);

            _onUpdate?.Invoke(curIndex);
            curIndex++;
        }
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


        public void Initialize(Transform _transform)
        {
            scroll = _transform.GetComponent<ScrollRect>();
            rtBaseItem = (RectTransform)scroll.content.GetChild(0);

            layout = scroll.content.GetComponent<HorizontalOrVerticalLayoutGroup>();
        }
    }
    #endregion VALIDATE

}
