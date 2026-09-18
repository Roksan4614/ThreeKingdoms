using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PopupTournament_Batch_Relic_Item : LobbyScreen_Hero_Relic_Item
{
    protected override async UniTask OnButtonAsync_Select(UnityAction<HeroInfoData> _onCallback)
    {
        await UniTask.Yield();

        var batchData = TournamentWorker.instance.GetBatchData(TournamentWorker.instance.isAttackType);

        if (batchData.treasure.Count >= 3 && m_heroInfoData.isBatch == false)
        {
            //최대_3개까지만_장착_가능합니다.
            PopupManager.instance.AlertShow_Table("RELIC_USE_MAX");
            return;
        }

        m_heroInfoData.isBatch = !m_heroInfoData.isBatch;

        TournamentWorker.instance.SetTreasureStatus(m_heroInfoData.skin, m_heroInfoData.isBatch);

        m_element.btn_select.SetDrawSelect(m_heroInfoData.isBatch);
        m_element.btn_select.text = TableManager.stringTable.GetString("BUTTON_CHOICE" + (m_heroInfoData.isBatch ? "_RUN":""));

        _onCallback(m_heroInfoData);
    }
}
