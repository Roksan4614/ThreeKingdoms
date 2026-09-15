using ThreeKingdoms.Client.Server;

public partial class PopupDailyDungeonResultComponent
{
    private bool IsServerResult => GameServer.Enabled && m_resultData?.serverRewards != null;

    private void SetServerResultText()
    {
        var killed = m_resultData.gradeType >= GradeType.Normal && m_resultData.gradeType < GradeType.MAX;
        m_element.txtResult.text = m_resultData.isSweep ? "토벌 완료" : killed ? "처치 성공" : "도전 종료";
        m_element.txtPercent.text = killed
            ? "최종 처치: [" + TableManager.stringTable.GetGradeType(m_resultData.gradeType, _isColor: true) + "]"
            : "보스를 처치하지 못했습니다.";
        if (m_rewards.Count == 0)
            m_element.txtPercent.text += "\n획득한 보상이 없습니다.";
        else if (!m_resultData.isSweep && m_resultData.gradeType < GradeType.Legend)
            m_element.txtPercent.text += $"\n{(killed ? "다음 보스" : "보스")} 피해량: {UnityEngine.Mathf.Clamp01(m_resultData.percent) * 100:0.00}%";
        m_element.pReward.gameObject.SetActive(m_rewards.Count > 0);
    }
}
