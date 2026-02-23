using UnityEngine;

public class PopupHeroSort : BasePopupComponent
{
    PopupHeroSort() : base(PopupType.Hero_Sort) { }

    public bool isNeedUpdate { get; private set; }
    public override void OpenPopup(params object[] _args)
    {
        isNeedUpdate = false;
        gameObject.SetActive(true);
    }

    public override void OnClose()
    {
        gameObject.SetActive(false);
    }
}
