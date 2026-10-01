using UnityEngine;

public class LobbyScreen_Shop_Group_SlotPass : LobbyScreen_Shop_Group_Slot
{
    protected override void Start()
    {
        base.Start();

        transform.Find("Panel/Outline").gameObject.SetActive(false);
    }
}
