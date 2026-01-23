using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class PopupManager : MonoBehaviour
{
    static PopupManager m_instance;
    public static PopupManager instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new GameObject().AddComponent<PopupManager>();
                m_instance.name = "PopupManager";
            }
            return m_instance;
        }
    }
    private void Awake()
    {
        m_instance = this;
    }

    private void OnDestroy()
    {
        m_instance = null;
    }
}
