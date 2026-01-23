using Unity.VisualScripting;
using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
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
