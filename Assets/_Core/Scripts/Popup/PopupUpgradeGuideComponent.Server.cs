using ThreeKingdoms.Client.Server;

public partial class PopupUpgradeGuideComponent
{
    private bool SetServerActionLabel(UpgradeGuideType type, ButtonHelper button)
    {
        if (!GameServer.Enabled) return false;
        if (type == UpgradeGuideType.TIMELOOP)
        {
            button.text = "다시 도전\n<color=#6f6f6f><size=55%>보상 없이 현재 전투를 다시 시작합니다.</size></color>";
            return true;
        }
        if (type == UpgradeGuideType.GACHA)
        {
            button.text = "연회 열기\n<color=#6f6f6f><size=55%>콘텐츠 준비 중입니다</size></color>";
            return true;
        }
        return false;
    }
}
