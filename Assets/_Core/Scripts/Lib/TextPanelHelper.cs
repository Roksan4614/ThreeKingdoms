using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextPanelHelper : MonoBehaviour, IValidatable
{
    [SerializeField] float m_fSpeed = 10;

    CancellationTokenSource m_cts;

    public string text
    {
        get => m_element.txt.text;
        set { m_element.txt.text = value; MovePanelAsync().Forget(); }
    }

    private void OnDestroy()
        => Release_CTS();

    async UniTask MovePanelAsync()
    {
        Release_CTS();
        m_cts = new();
        var token = m_cts.Token;

        m_element.panel.ForceRebuildLayout();

        // 사이즈가 안에 있다면,
        if (m_element.width >= m_element.panel.sizeDelta.x)
        {
            var pos = m_element.panel.anchoredPosition;
            pos.x = m_element.width * 0.5f - m_element.panel.sizeDelta.x * 0.5f;
            m_element.panel.anchoredPosition = pos;

            return;
        }

        Instantiate(m_element.txt, m_element.panel);
        m_element.panel.ForceRebuildLayout();
        m_element.panel.anchoredPosition = Vector2.zero;

        await UniTask.WaitForSeconds(2f, cancellationToken: token);

        var moveWidth = m_element.panel.sizeDelta.x * -0.5f - m_element.spacing * 0.5f;

        while (true)
        {
            var pos = m_element.panel.anchoredPosition;
            pos.x -= m_fSpeed * Time.deltaTime;

            if (pos.x < moveWidth)
            {
                pos.x -= moveWidth;
                await UniTask.WaitForSeconds(2f, cancellationToken: token);
            }

            m_element.panel.anchoredPosition = pos;

            await UniTask.WaitForEndOfFrame(cancellationToken: token);
        }
    }

    void Release_CTS()
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public RectTransform panel;
        public TextMeshProUGUI txt;

        public float width;
        public float spacing;

        public void Initialize(Transform _transform)
        {
            width = ((RectTransform)_transform).rect.width;
            panel = (RectTransform)_transform.Find("Panel");
            txt = panel.GetComponent<TextMeshProUGUI>("Text");

            spacing = panel.GetComponent<HorizontalLayoutGroup>().spacing;
        }
    }
    #endregion VALIDATE

}
