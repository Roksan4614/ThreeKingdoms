using ThreeKingdoms.Client.Server;

public partial class PopupRebirthComponent
{
    private void SetServerRebirthInfo(bool available)
    {
        if (!GameServer.Enabled) return;
        m_element.txtTitle.text = "스테이지 되돌리기";
        m_element.txtRewardTitle.text = "보상 없음";
        m_element.txtRewardCount.text = "-";
        m_element.btnConfirm.text = "되돌리기";
        if (available) m_element.txtDesc.text = "스테이지 진행만 되돌립니다. 보상은 지급되지 않습니다.";
    }
}
