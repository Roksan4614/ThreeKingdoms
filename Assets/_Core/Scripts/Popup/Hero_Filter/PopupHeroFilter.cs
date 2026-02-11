using UnityEngine;
using UnityEngine.UI;

public class PopupHeroFilter : BasePopupComponent
{
    PopupHeroFilter() : base(PopupType.Hero_Filter) { }

    public int aa = 0;

    private void Awake()
    {
        transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(OnClose);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            aa++;
    }
}
