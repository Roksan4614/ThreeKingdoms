using System;
using UnityEngine;

public class LobbyScreen_Castle_Popup_Setting_UpgradeInfo : MonoBehaviour, IValidatable
{
    TableCastleRiseData m_nowData;
    TableCastleRiseData m_nextData;
    Data_Castle.CastleData m_castleData;

    public void SetUpgradeInfo(Data_Castle.CastleData _castleData)
    {
        m_castleData = _castleData;

        if (_castleData.level == 10)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        m_nowData = TableManager.castleRise.GetRiseData(_castleData.type, _castleData.level);
        m_nextData = TableManager.castleRise.GetRiseData(_castleData.type, _castleData.level + 1);

        int i = _castleData.type switch
        {
            CastleObjectType.Palace => SetInfo_Palace(),
            CastleObjectType.Farm => SetInfo_FarmMarket(),
            CastleObjectType.Market => SetInfo_FarmMarket(),
            CastleObjectType.Office => SetInfo_Office(),
            CastleObjectType.Merchant => SetInfo_Merchant(),
            CastleObjectType.Gate => SetInfo_Gate(),
            _ => SetInfo_Gate()
        };

        // 공통
        {
            // 고유 능력 요구치
            SetAddItem(i++, "고유_능력_요구치", m_nowData.value_01.ToString(), m_nextData.value_01.ToString());

            // 장수배치 수
            SetAddItem(i++, "배치_장수_수", m_nowData.count_batch.ToString(), m_nextData.count_batch.ToString());
        }

        for (; i < m_element.panel.childCount; i++)
            m_element.panel.GetChild(i).gameObject.SetActive(false);

        m_nextData = m_nowData = default;

        m_element.panel.ForceRebuildLayout();
    }

    void SetAddItem(int _idx, string _name, string _now, string _after)
    {
        var item = _idx == m_element.panel.childCount
            ? Instantiate(m_element.panel.GetChild(0), m_element.panel)
            : m_element.panel.GetChild(_idx);
        item.gameObject.SetActive(true);

        item.SetText("txt_name", _name);
        item.SetText("txt_value", $"{_now}  <size=70%>></size>  <color=#{Palette.htmlString_Up}>{_after}" );
    }

    int SetInfo_Palace()
    {
        int i = 0;
        return i;
    }
    int SetInfo_FarmMarket()
    {
        int i = 0;
        var nextCastleData = m_castleData;
        nextCastleData.level += 1;

        SetAddItem(i++, "초당_획득량",
            $"{DataManager.castle.GetAmountPerSecond(m_castleData).AmountKMBT()}/s",
            $"{DataManager.castle.GetAmountPerSecond(nextCastleData).AmountKMBT()}/s");

        SetAddItem(i++, "보유량 한도",
            $"{DataManager.castle.GetMaxAmount(m_castleData).AmountKMBT()}",
            $"{DataManager.castle.GetMaxAmount(nextCastleData).AmountKMBT()}");

        return i;
    }
    int SetInfo_Office() { int i = 0; return i; }
    int SetInfo_Merchant() { int i = 0; return i; }
    int SetInfo_Gate() { int i = 0; return i; }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform panel;
        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
        }
    }
    #endregion VALIDATE

}
