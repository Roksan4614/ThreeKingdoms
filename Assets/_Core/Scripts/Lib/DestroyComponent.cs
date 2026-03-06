using UnityEngine;

public class DestroyComponent : MonoBehaviour
{
    private void Awake()
    {
        Destroy(gameObject);
    }
}
