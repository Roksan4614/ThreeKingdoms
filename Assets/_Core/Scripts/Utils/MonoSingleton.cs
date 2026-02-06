using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T instance;

    void Awake()
    {
        instance = this as T;
        OnAwake();
    }

    protected virtual void OnAwake() { }

    protected virtual void OnDestroy()
    {
        instance = null;
    }
}

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    static T m_instance;
    public static T instance
    {
        get
        {
            if (m_instance == null)
                new GameObject("S_" + typeof(T).ToString(), typeof(T)).GetComponent<T>();
            return m_instance;
        }
    }

    void Awake()
    {
        if (m_instance != null)
        {
            Debug.Log("MonoSingleton: DELETE: " + transform.GetHierarchyPath());
            Destroy(gameObject);
            return;
        }
        else
            m_instance = this as T;

        DontDestroyOnLoad(gameObject);

        OnAwake();
    }

    protected virtual void OnAwake() { }

    protected virtual void OnDestroy()
    {
        m_instance = null;
    }
}