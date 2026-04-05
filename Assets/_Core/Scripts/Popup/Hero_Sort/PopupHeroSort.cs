using UnityEngine;
using UnityEngine.UI;

public class PopupHeroSort : BasePopupComponent
{
    PopupHeroSort() : base(PopupType.Hero_Sort) { }
    protected override void Awake()
    {
        transform.GetComponent<Button>("Area/Panel/btn_close")?.onClick.AddListener(Close);
    }
    public bool isNeedUpdate { get; private set; }

    public override void OpenPopup(params object[] _args)
    {
        isNeedUpdate = false;
        gameObject.SetActive(true);
    }

    public override void Close()
    {
        gameObject.SetActive(false);
    }

    public override void OnManualValidate()
    {
    }
}
