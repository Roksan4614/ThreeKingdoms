using System;
using Cysharp.Threading.Tasks;
using ThreeKingdoms.Client.Server;
using ThreeKingdoms.Shared.Enums;
using TMPro;

public partial class PopupHeroInfo_Popup_Upgrade
{
    private bool m_serverConfirming;
    private void SetServerInfo(UpgradeType type)
    {
        var current = ServerState.Character(m_heroInfoData.key);
        if (current == null) { m_element.btnConfirm.interactable = false; return; }
        var amount = m_element.btnConfirm.transform.Find("Amount/Text").GetComponent<TextMeshProUGUI>();
        if (type == UpgradeType.Upgrade)
        {
            amount.text = CharacterActions.AscensionCost(m_heroInfoData.key, m_heroInfoData.grade).ToString("#,0");
            m_element.btnConfirm.interactable = (int)m_heroInfoData.grade > (int)current.CharacterGrade;
        }
        else
        {
            var next = current.NextGrowth;
            m_element.btnConfirm.interactable = next != null;
            amount.text = next == null ? "MAX" : next.CostRice.ToString("#,0") + " 군량";
            m_element.txtTitle.text = next == null ? "최대 성장 단계" : "성장 성공 확률: " + (next.IsGuaranteed ? "100% (보장)" : (next.SuccessRate * 100).ToString("0.##") + "%");
            var icon = m_element.btnConfirm.transform.Find("Amount/Icon/Gold");
            if (icon != null) icon.gameObject.SetActive(false);
            if (double.TryParse(current.GrowthPoint, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var points))
                m_element.rtBar.anchorMax = new UnityEngine.Vector2((float)Math.Max(0, Math.Min(1, points / 100)), m_element.rtBar.anchorMax.y);
        }
    }
    private async UniTask ConfirmServerAsync()
    {
        if (m_serverConfirming) return;
        m_serverConfirming = true;
        m_element.btnConfirm.interactable = false;
        try
        {
            if (m_element.parentUpgrade.gameObject.activeSelf)
            {
                await CharacterActions.AscendAsync(m_heroInfoData.key, m_heroInfoData.grade);
                PopupManager.instance.AlertShow("승급이 완료되었습니다.");
            }
            else
            {
                var result = await CharacterActions.GrowAsync(m_heroInfoData.key);
                PopupManager.instance.AlertShow(result.GrowthResult == CharacterGrowthResult.Fail ? "성장에 실패했습니다. 성장 포인트가 누적되었습니다." : "성장에 성공했습니다.");
            }
            m_heroInfoData = ServerState.ToHero(ServerState.Character(m_heroInfoData.key));
            m_status = StatusType.Success;
            m_serverConfirming = false;
            Close();
        }
        catch (Exception error) { PopupManager.instance.AlertShow(error.Message); }
        finally
        {
            m_serverConfirming = false;
            if (gameObject.activeSelf) SetServerInfo(m_element.parentUpgrade.gameObject.activeSelf ? UpgradeType.Upgrade : UpgradeType.Enchant);
        }
    }
}
