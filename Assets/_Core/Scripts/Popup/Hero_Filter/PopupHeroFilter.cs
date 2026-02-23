using UnityEngine;
using UnityEngine.UI;

public class PopupHeroFilter : BasePopupComponent
{
    PopupHeroFilter() : base(PopupType.Hero_Filter) { }

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
}
