using UnityEngine;
using UnityEngine.Rendering.Universal;

// Only the Boot scene's persistent global light carries this ownership marker.
// Run before Light2D.OnEnable so a repeated Boot cannot register another light.
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Light2D))]
public sealed class BootGlobalLight : MonoBehaviour
{
    private static BootGlobalLight owner;

    private void Awake() => ClaimOwnership();
    private void OnEnable() => ClaimOwnership();

    private void ClaimOwnership()
    {
        if (owner != null && owner != this)
        {
            // Destroy is deferred; disabling first prevents registration/rendering
            // during the remainder of the scene-load frame.
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
