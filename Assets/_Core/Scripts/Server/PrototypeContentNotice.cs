namespace ThreeKingdoms.Client.Server
{
    public static class PrototypeContentNotice
    {
        public static bool ShowIfServer()
        {
            if (!GameServer.Enabled) return false;
            PopupManager.instance.AlertShow("콘텐츠 준비 중입니다");
            return true;
        }
    }
}
