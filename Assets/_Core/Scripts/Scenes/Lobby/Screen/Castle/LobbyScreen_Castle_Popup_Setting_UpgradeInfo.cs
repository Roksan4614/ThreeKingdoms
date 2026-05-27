using System;
using UnityEngine;

public class LobbyScreen_Castle_Popup_Setting_UpgradeInfo : MonoBehaviour, IValidatable
{
    TableCastleRiseData m_nowData;
    TableCastleRiseData m_nextData;
    Data_Castle.CastleData m_castleData;

    bool m_isOpenGateInfo;

    private void Start()
    {
        Signal.instance.UpdateCastleHeroBatch.connectLambda = new(this, _ =>
        {
            if (gameObject.activeInHierarchy == true && m_isOpenGateInfo == true)
                SetGateInfo(DataManager.castle.GetCaslteData(CastleObjectType.Gate));
        });
    }

    public void SetUpgradeInfo(Data_Castle.CastleData _castleData)
    {
        m_castleData = _castleData;

        if (_castleData.level == 10)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        m_isOpenGateInfo = false;

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
            SetAddItem(i++, "고유_능력_요구치",
                (m_castleData.type == CastleObjectType.Palace ? m_nowData.orinValue01 : m_nowData.value01).ToString(),
                (m_castleData.type == CastleObjectType.Palace ? m_nextData.orinValue01 : m_nextData.value01).ToString());

            // 장수배치 수
            SetAddItem(i++, "배치_장수_수", m_nowData.character_slot_max.ToString(), m_nextData.character_slot_max.ToString());

            // 업그레이드 시간
            // 업그레이드 중이면 안보여주자
            if (m_castleData.isDoingUpgrade == false)
            {
                var dt = DateTime.Now;
                var ts = dt.AddSeconds(m_castleData.dbRise.upgradeSeconds) - dt;
                SetAddItem(i++, "증축_시간", m_castleData.dbRise.upgradeSeconds == 0 ? "_즉시" :
                    ts.Days > 0 ?
                    $"{ts.Days}d {ts.Hours}h" :
                    ts.Hours > 0 ?
                    $"{ts.Hours}h {ts.Minutes}m" :
                    ts.Minutes > 0 ?
                    $"{ts.Minutes}m {ts.Seconds}s" :
                    $"{ts.TotalSeconds:0.##}s", null);
            }
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
        item.SetText("txt_value", $"{_now}{(_after.IsActive() ? $"  <size=70%>></size>  <color=#{Palette.htmlString_Up}>{_after}" : "")}");
    }

    int SetInfo_Palace()
    {
        var nowData = TableManager.castleEffect[m_castleData.type].Get(m_castleData.level);
        var nextData = TableManager.castleEffect[m_castleData.type].Get(m_castleData.level + 1);

        int i = 0;

        var probity = DataManager.castle.GetGateProbityRate();

        SetAddItem(i++, "건물_레벨_상한선", $"{nowData.level_cap ?? -1}", $"{nextData.level_cap ?? -1}");
        SetAddItem(i++, "시간석_개당_단축", $"{(nowData.time_stone_sec * probity ?? -1):0.##}s",
            $"{(nextData.time_stone_sec * probity ?? -1):0.##}s");
        SetAddItem(i++, "광고_회당_단축", $"{nowData.ad_reduce_min ?? -1}s", $"{nextData.ad_reduce_min ?? -1}s");

        return i;
    }
    int SetInfo_FarmMarket()
    {
        var nextCastleData = m_castleData;
        nextCastleData.level += 1;

        int i = 0;

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

    public void SetGateInfo(Data_Castle.CastleData _castleData)
    {
        m_isOpenGateInfo = true;

        m_castleData = _castleData;
        gameObject.SetActive(true);

        var probity = DataManager.castle.GetGateProbityRate();

        int i = 0;

        // 궁성
        {
            var timeStoneSec = TableManager.castleEffect[CastleObjectType.Palace].Get(m_castleData.level).time_stone_sec ?? -1;
            SetAddItem(i++, "시간석_개당_단축", $"{timeStoneSec}s", $"{(timeStoneSec * probity):0.##}s");
        }
        // 농장
        {
            var perSecond = DataManager.castle.GetAmountPerSecond(DataManager.castle.GetCaslteData(CastleObjectType.Farm), false);

            SetAddItem(i++, "금화_초당_획득량", $"{perSecond.AmountKMBT()}/s",
                $"{(perSecond * probity).AmountKMBT()}/s");
        }
        // 상점
        {
            var perSecond = DataManager.castle.GetAmountPerSecond(DataManager.castle.GetCaslteData(CastleObjectType.Market), false);

            SetAddItem(i++, "군량_초당_획득량", $"{perSecond.AmountKMBT()}/s",
                $"{(perSecond * probity).AmountKMBT()}/s");
        }

        for (; i < m_element.panel.childCount; i++)
            m_element.panel.GetChild(i).gameObject.SetActive(false);

        m_element.panel.ForceRebuildLayout();
    }

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
