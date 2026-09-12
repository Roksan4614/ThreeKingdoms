using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using ThreeKingdoms.Client.Server;
using UnityEngine;

public partial class LobbyScreen_Castle_Popup_Setting
{
    private bool serverDebuffPending;
    private string serverDebuffError;

    private void OnGUI()
    {
        if (!GameServer.Enabled || m_castleData?.type != CastleObjectType.Gate || !gameObject.activeInHierarchy) return;
        if (DataManager.castle.ServerCastle?.Thief?.DebuffActive != true && string.IsNullOrEmpty(serverDebuffError)) return;
        var oldEnabled = GUI.enabled;
        GUI.enabled = !serverDebuffPending;
        if (GUI.Button(new Rect(Screen.width - 250, 80, 230, 44), serverDebuffPending ? "해제 요청 중..." : "도둑 디버프 해제")) ClearServerDebuffAsync().Forget();
        GUI.enabled = oldEnabled;
        if (!string.IsNullOrEmpty(serverDebuffError)) GUI.Label(new Rect(Screen.width - 420, 130, 400, 50), "해제 실패: " + serverDebuffError);
    }

    private async UniTask ClearServerDebuffAsync()
    {
        serverDebuffPending = true;
        serverDebuffError = null;
        try { await DataManager.castle.ClearServerDebuffAsync(); }
        catch (Exception error) { serverDebuffError = error is GameServerException server ? server.Code : error.Message; }
        finally { serverDebuffPending = false; }
    }

    private void SetServerCoreStatInfo()
    {
        m_logUpgrade = "";
        var upgradeRequirements = DataManager.castle.ServerUpgradeRequirements(m_castleData.type);
        for (var index = 0; index < m_element.txtBatchStat.Length; index++)
        {
            var text = m_element.txtBatchStat[index];
            var requirement = upgradeRequirements.ElementAtOrDefault(index);
            text.gameObject.SetActive(requirement != null);
            if (requirement == null) continue;
            text.text = "다음 레벨 " + requirement.StatType + ": " + requirement.CurrentValue + "/" + requirement.RequiredValue;
        }
        if (m_castleData.type == CastleObjectType.Office)
        {
            var office = DataManager.castle.mission.levelInfo;
            m_element.txtBatchStat[0].gameObject.SetActive(true);
            m_element.txtBatchStat[0].text = "XP: " + office.nowExp + "/" + office.maxExp;
        }
        m_element.btnUpgrade.text = m_castleData.type == CastleObjectType.Office ? "임무 XP로 자동 성장" : DataManager.castle.CanServerUpgrade(m_castleData.type) ? "증축 시작" : "증축 조건 미달";
        m_element.btnUpgrade.interactable = m_castleData.type != CastleObjectType.Office && DataManager.castle.CanServerUpgrade(m_castleData.type) && !m_castleData.isDoingUpgrade;
        m_element.txtBatchStat[0].transform.parent.ForceRebuildLayout();
        if (m_element.gauge.gameObject.activeSelf)
            m_element.txtPerSecond.text = "초당 생산량: " + DataManager.castle.ServerProductionRate(m_castleData.type).AmountKMBT();
    }
}

public partial class LobbyScreen_Castle_NPC_Wally
{
    private bool serverCapturePending;
    private async UniTask CaptureServerAsync()
    {
        if (serverCapturePending) return;
        serverCapturePending = true;
        try
        {
            var captured = await DataManager.castle.CaptureServerThiefAsync();
            if (captured)
            {
                Release_CTS();
                m_isShow = false;
                m_element.anim.Play("Castle_Wally_Hit");
            }
            else if (DataManager.castle.wallyData.isSpawn)
            {
                m_element.anim.Play("Castle_Wally_End");
                SpawnStartAsync(true).Forget();
            }
        }
        finally { serverCapturePending = false; }
    }
}
