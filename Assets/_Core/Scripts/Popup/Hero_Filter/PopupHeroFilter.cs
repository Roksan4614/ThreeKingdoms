using UnityEngine;
using UnityEngine.UI;

public class PopupHeroFilter : BasePopupComponent
{
    PopupHeroFilter() : base(PopupType.Hero_Filter) { }

    public override void OpenPopup(params object[] _args)
    {
        gameObject.SetActive(true);
    }

    public override void OnClose()
    {
        gameObject.SetActive(false);
    }
}
