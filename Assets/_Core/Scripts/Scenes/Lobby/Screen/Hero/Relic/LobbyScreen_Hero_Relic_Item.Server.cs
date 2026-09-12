using System;
using Cysharp.Threading.Tasks;
using ThreeKingdoms.Client.Server;
using UnityEngine.Events;

public partial class LobbyScreen_Hero_Relic_Item
{
    private bool m_serverMutating;
    private async UniTask UpgradeServerRelicAsync(UnityAction<HeroInfoData> onUpdate)
    {
        if (m_serverMutating) return;
        m_serverMutating = true;
        m_element.btn_enchant.interactable = false;
        try
        {
            m_heroInfoData = await CharacterActions.EnhanceRelicAsync(m_heroInfoData.key);
            SetRelicData(m_heroInfoData, true);
            onUpdate?.Invoke(m_heroInfoData);
        }
        catch (Exception error) { PopupManager.instance.AlertShow(error.Message); }
        finally { m_serverMutating = false; SetServerRelicInfo(m_heroInfoData); }
    }
    private async UniTask SelectServerTreasureAsync(UnityAction<HeroInfoData> onUpdate)
    {
        if (m_serverMutating) return;
        m_serverMutating = true;
        m_element.btn_select.interactable = false;
        try
        {
            var equipped = !m_heroInfoData.isBatch;
            await CharacterActions.SetTreasureAsync(m_heroInfoData.skin, equipped);
            m_heroInfoData.isBatch = equipped;
            m_element.btn_select.SetDrawSelect(equipped);
            m_element.btn_select.text = equipped ? "장착 중" : "장착";
            onUpdate?.Invoke(m_heroInfoData);
        }
        catch (Exception error) { PopupManager.instance.AlertShow(error.Message); }
        finally { m_serverMutating = false; m_element.btn_select.interactable = m_heroInfoData.isMine; }
    }
    private void SetServerRelicInfo(HeroInfoData hero)
    {
        var snapshot = ServerState.Character(hero.key);
        if (snapshot == null) { m_element.btn_enchant.interactable = false; return; }
        var next = snapshot.Relic.NextEnhance;
        m_element.txt_stat.text = $"최종 전투력: {snapshot.CombatPower:#,0}\n현재 등급 강화 상한: {snapshot.Relic.MaxEnhanceStage}";
        m_element.btn_enchant.text = next == null ? "MAX" : $"강화 ({next.CostTimeStone:#,0} 시간석)";
        m_element.btn_enchant.interactable = next != null && !m_serverMutating;
    }
}
