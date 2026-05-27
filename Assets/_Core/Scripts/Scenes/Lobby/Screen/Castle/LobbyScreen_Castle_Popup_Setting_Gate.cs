using UnityEngine;
using static Data_Castle;

public class LobbyScreen_Castle_Popup_Setting_Gate : LobbyScreen_Castle_Popup_Setting_Palace
{
    protected override void Start()
    {
        Signal.instance.UpdateCastleHeroBatch.connect = SlotUpdateCastleHeroBatch;
    }

    protected override void OnEnable()
    {
        SlotUpdateCastleHeroBatch(DataManager.castle.GetCaslteData(CastleObjectType.Gate));
    }

    void SlotUpdateCastleHeroBatch(Data_Castle.CastleData _castleData)
    {
        if (gameObject.activeInHierarchy == false || _castleData.type != CastleObjectType.Gate)
            return;

        var dbCastle = TableManager.castle.GetCastleData(_castleData.type);
        var dbCastleRise = _castleData.dbRise;

        for (int i = 0; i < dbCastle.coreStat.Length; i++)
        {
            var coreStat = dbCastle.coreStat[i];

            var total = DataManager.castle.GetTotalCoreStat(_castleData, coreStat);
            var max = dbCastleRise.maxCoreStat[i];

            string format = i == 0 ? "청렴도_{0}%" : "치안율_{0}%";
            m_element.gauge[i].textTitle = string.Format(format, total == 0 ? "0" : $"+{(Mathf.Min(1, total / (float)max) * 0.45f) * 100:0.##}");
            m_element.gauge[i].textAmount = $"{total:#,0} / {max:#,0}";
            m_element.gauge[i].fillAmount = total == 0 ? 0 : (total / (float)max);
        }
    }
}
