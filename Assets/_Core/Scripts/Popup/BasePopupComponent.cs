using UnityEngine;

public class BasePopupComponent : MonoBehaviour
{
    public PopupType popupType { get; private set; }
    protected BasePopupComponent(PopupType _popupType) => popupType = _popupType;

    public virtual void OpenPopup(params object[] _args) { }

    public void OnClose()
    {
        OnClosePopup();
        Destroy(gameObject);
    }

    protected virtual void OnClosePopup() { }

}
