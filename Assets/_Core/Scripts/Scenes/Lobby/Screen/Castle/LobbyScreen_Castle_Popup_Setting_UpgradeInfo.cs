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

        SetAddItem(0, $"<size=110%><color=#000000>Lv.{_castleData.level + 1} 효과</color></size>", null, null);

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
            SetAddItem(i++, " 고유_능력_요구치",
                (m_castleData.type == CastleObjectType.Palace ? m_nowData.orinValue01 : m_nowData.value01).ToString(),
                (m_castleData.type == CastleObjectType.Palace ? m_nextData.orinValue01 : m_nextData.value01).ToString());

            // 장수배치 수
            SetAddItem(i++, " 배치_장수_수", m_nowData.character_slot_max.ToString(), m_nextData.character_slot_max.ToString());

            // 업그레이드 시간
            // 업그레이드 중이면 안보여주자.. 보여주까??
            //if (m_castleData.isDoingUpgrade == false)
            {
                var dt = DateTime.Now;
                var ts = dt.AddSeconds(m_castleData.dbRise.upgradeSeconds) - dt;
                SetAddItem(i++, " 증축_시간", m_castleData.dbRise.upgradeSeconds == 0 ? "_즉시" :
                    ts.ToRemainTime(_isStringMode: true), null);
            }
        }

        for (; i < m_element.panel.childCount; i++)
            m_element.panel.GetChild(i).gameObject.SetActive(false);

        m_nextData = m_nowData = default;

        m_element.panel.ForceRebuildLayout();
    }

    void SetAddItem(int _idx, string _name, string _now, string _after, bool _isUp = true)
    {
        var item = _idx == m_element.panel.childCount
            ? Instantiate(m_element.panel.GetChild(0), m_element.panel)
            : m_element.panel.GetChild(_idx);
        item.gameObject.SetActive(true);

        item.SetText("txt_name", _name);
        item.SetText("txt_value", $"{(_now.IsActive() ? _now : "")}{(_after.IsActive() ? $"  <size=70%>></size>  <color=#{(_isUp ? Palette.htmlString_Up : Palette.htmlString_Down)}>{_after}" : "")}");
    }

    int SetInfo_Palace()
    {
        var nowData = TableManager.castleEffect[m_castleData.type].Get(m_castleData.level);
        var nextData = TableManager.castleEffect[m_castleData.type].Get(m_castleData.level + 1);

        int i = 1;

        var probity = DataManager.castle.GetGateProbityRate();

        SetAddItem(i++, " 건물_레벨_상한선", $"{nowData.level_cap ?? -1}", $"{nextData.level_cap ?? -1}");
        SetAddItem(i++, " 시간석_개당_단축", $"{(nowData.time_stone_sec * probity ?? -1):0.##}s",
            $"{(nextData.time_stone_sec * probity ?? -1):0.##}s");
        SetAddItem(i++, " 광고_회당_단축", $"{nowData.ad_reduce_min ?? -1}s", $"{nextData.ad_reduce_min ?? -1}s");

        return i;
    }
    int SetInfo_FarmMarket()
    {
        var nextCastleData = m_castleData;
        nextCastleData.level += 1;

        int i = 1;

        SetAddItem(i++, " 초당_획득량",
            $"{DataManager.castle.GetAmountPerSecond(m_castleData).AmountKMBT()}/s",
            $"{DataManager.castle.GetAmountPerSecond(nextCastleData).AmountKMBT()}/s");

        SetAddItem(i++, " 보유량_한도",
            $"{DataManager.castle.GetMaxAmount(m_castleData).AmountKMBT()}",
            $"{DataManager.castle.GetMaxAmount(nextCastleData).AmountKMBT()}");

        return i;
    }
    int SetInfo_Office() { int i = 0; return i; }
    int SetInfo_Merchant() { int i = 0; return i; }
    int SetInfo_Gate()
    {
        int i = 0;
        var nextLevel = m_castleData.level + 1;

        SetAddItem(i++, " 도적_유지_시간",
            $"{TableManager.castleEffect[m_castleData.type].Get(m_castleData.level).npc_duration_sec}/s",
            $"{TableManager.castleEffect[m_castleData.type].Get(nextLevel).npc_duration_sec}/s");
        return i;
    }

    //public void SetPalaceInfo(Data_Castle.CastleData _castleData)
    //{
    //    gameObject.SetActive(true);

    //    int i = 0;

    //    SetAddItem(i++, "오늘_획득한 금화", DataManager.castle.GetCaslteData(CastleObjectType.Market).todayClaimAmount.ToString("#,0"), null);
    //    SetAddItem(i++, "오늘_획득한 군량", DataManager.castle.GetCaslteData(CastleObjectType.Farm).todayClaimAmount.ToString("#,0"), null);

    //    for (; i < m_element.panel.childCount; i++)
    //        m_element.panel.GetChild(i).gameObject.SetActive(false);

    //    m_element.panel.ForceRebuildLayout();

    //}

    public void SetGateInfo(Data_Castle.CastleData _castleData)
    {
        m_isOpenGateInfo = true;

        m_castleData = _castleData;
        gameObject.SetActive(true);

        var probity = DataManager.castle.GetGateProbityRate();

        int i = 0;

        SetAddItem(i++, "<size=110%><color=#000000>청렴도 영향</color></size>", null, null);
        // 궁성
        {
            var levelPalace = DataManager.castle.GetCaslteData(CastleObjectType.Palace).level;
            var timeStoneSec = TableManager.castleEffect[CastleObjectType.Palace].Get(levelPalace).time_stone_sec ?? -1;
            SetAddItem(i++, " 시간석_개당_단축", $"{timeStoneSec}s", probity == 1 ? null : $"{(timeStoneSec * probity):0.##}s", false);
        }
        // 상점
        {
            SetAddItem(i++, " 군량/금화_수령시_획득량", $"<color=#{(probity < 1 ? Palette.htmlString_Down : Palette.htmlString_Up)}>{(probity) * 100: 0.##}%", null);
        }
        // 행상
        {
            // todo
            var discountRate = 0.1f;
            SetAddItem(i++, " 상점 할인율", $"{discountRate * 100: 0.##}%", probity == 1 ? null : $"{discountRate * probity * 100: 0.##}%", false);
        }
        // 관아
        {
            if (probity < 1)
            {
                var orinProbity = DataManager.castle.GetGateProbityRate(true);
                SetAddItem(i++, " 높은 등급 등장 확률 감소", $"<color=#{Palette.htmlString_Down}>-{(1 - orinProbity) * 100: 0.##}%", null);
            }
            else
                SetAddItem(i++, " 높은 등급 등장 확률 감소", $"0%", null);
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
