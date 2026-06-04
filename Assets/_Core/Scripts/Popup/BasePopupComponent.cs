using UnityEngine;
using UnityEngine.UI;

public abstract class BasePopupComponent : MonoBehaviour, IValidatable
{
    public PopupType popupType { get; private set; }
    protected BasePopupComponent(PopupType _popupType) => popupType = _popupType;

    GameObject m_dimm;
    protected GameObject dimm => m_dimm;

    protected virtual void Awake()
    {

        transform.GetComponent<Button>("Panel/btn_close")?.onClick.AddListener(Close);
        var btnDimm = transform.GetComponent<Button>("Dimm");
        if (btnDimm != null)
        {
            btnDimm.onClick.AddListener(Close);
            m_dimm = btnDimm.gameObject;
        }
    }

    public virtual void OpenPopup(params object[] _args) { }

    public virtual void Close()
    {
        OnClosePopup();
        Destroy(gameObject);
    }

    protected virtual void OnClosePopup() { }

    public abstract void OnManualValidate();
}
