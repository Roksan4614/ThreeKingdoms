using UnityEngine;

public class DontDestroyComponent : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
        Destroy(this);
    }
}
