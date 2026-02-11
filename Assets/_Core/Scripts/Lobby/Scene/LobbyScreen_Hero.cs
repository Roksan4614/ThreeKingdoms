using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Hero : LobbyScreen_Base
{
    PopupHeroFilter m_popup;

    private void Start()
    {
        transform.GetComponent<Button>("Panel/Wait/btn_filter").onClick.AddListener(
            async () =>
            {
                m_popup = await PopupManager.instance.OpenPopupAndWait<PopupHeroFilter>(PopupType.Hero_Filter);
            });
    }
}
