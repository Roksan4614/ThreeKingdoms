using UnityEngine;

public class ItemBattlePassSeason1 : MonoBehaviour
{
    private void Start()
    {
        transform.SetText("Rewards/txt_main", TableManager.stringShop.GetString("UI_BATTLE_PASS_1_REWARD_TITLE"));
        transform.SetText("Rewards/txt_sub", TableManager.stringShop.GetString("UI_BATTLE_PASS_1_REWARD_SUB"));
        transform.SetText("txt_desc", TableManager.stringShop.GetString("UI_BATTLE_PASS_1_REWARD_DESC"));
    }
}
