using UnityEngine;
using UnityEngine.EventSystems;

// Only the Boot scene's persistent EventSystem carries this ownership marker.
// Run before EventSystem.OnEnable so a repeated Boot cannot register another input system.
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(EventSystem))]
public sealed class BootEventSystem : MonoBehaviour
{
    private static BootEventSystem owner;

    private void Awake() => ClaimOwnership();
    private void OnEnable() => ClaimOwnership();

    private void ClaimOwnership()
    {
        if (owner != null && owner != this)
        {
            // Destruction is deferred; stop input registration during the rest of this load frame.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        owner = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (owner == this) owner = null;
    }
}
