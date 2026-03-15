using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T instance;

    protected virtual void Awake()
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
            Destroy(gameObject);
            return;
        }
        else
            m_instance = this as T;

        DontDestroyOnLoad(gameObject);

        transform.SetSiblingIndex(1);

        OnAwake();
    }

    protected virtual void OnAwake() { }

    protected virtual void OnDestroy()
    {
        if (m_instance == this)
            m_instance = null;
    }
}