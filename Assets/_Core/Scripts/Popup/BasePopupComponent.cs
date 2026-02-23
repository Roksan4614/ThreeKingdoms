using UnityEngine;
using UnityEngine.UI;

public class BasePopupComponent : MonoBehaviour
{
    public PopupType popupType { get; private set; }
    protected BasePopupComponent(PopupType _popupType) => popupType = _popupType;

    protected virtual void Awake()
    {
        transform.GetComponent<Button>("Panel/btn_close")?.onClick.AddListener(Close);
    }

    public virtual void OpenPopup(params object[] _args) { }

    public virtual void Close()
    {
        OnClosePopup();
        Destroy(gameObject);
    }

    protected virtual void OnClosePopup() { }

}
