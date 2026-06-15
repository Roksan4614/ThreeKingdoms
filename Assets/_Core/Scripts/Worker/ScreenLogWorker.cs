using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScreenLogWorker : Singleton<ScreenLogWorker>, IValidatable
{
    Queue<LogData> m_logQueue = new Queue<LogData>();

    List<TextMeshProUGUI> m_lstText = new();

    protected override void OnAwake()
    {
        m_lstText.Add(m_element.txtBase);
    }

    private void LateUpdate()
    {
        int i = 0;
        while (m_logQueue.Count > 0)
        {
            if (m_lstText.Count == i)
            {
                m_lstText.Add(Instantiate(m_element.txtBase, transform));
                transform.ForceRebuildLayout();
            }

            var logData = m_logQueue.Dequeue();
            m_lstText[i].text = $"[{logData.key}] {logData.message}";
            m_lstText[i].gameObject.SetActive(true);
            i++;
        }

        for (; i < m_lstText.Count; i++)
            m_lstText[i].gameObject.SetActive(false);
    }

    public static void Add(string _key, object _message)
    {
        instance.m_logQueue.Enqueue(new LogData() { key = _key, message = _message.ToString() });
    }

    struct LogData
    {
        public string key;
        public string message;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtBase;

        public void Initialize(Transform _transform)
        {
            txtBase = _transform.GetComponent<TextMeshProUGUI>("txt_log");
        }
    }
    #endregion VALIDATE

}
