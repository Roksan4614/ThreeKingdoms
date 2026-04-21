using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo_Stat_Battle : MonoBehaviour, IValidatable
{
    Dictionary<StatType, StatData> m_dbStat = new();
    HeroInfoData m_heroInfoData;

    private void Awake()
    {
        m_dbStat = m_element.lstStat.ToDictionary(x => x.type, x => x);
        m_element.lstStat = null;
    }

    public void SetActive(bool _isActive)
    {
        gameObject.SetActive(_isActive);
    }

    public void SetStatData(HeroInfoData _heroData)
    {
        m_heroInfoData = _heroData;
        var statString = DataManager.stat.GetResultStat(_heroData).statString;

        foreach (var s in statString)
            m_dbStat[s.Key].txtValue.text = s.Value;
    }

    public void SetCompareData(HeroInfoData _next)
    {
        var prev = DataManager.stat.GetResultStat(m_heroInfoData);
        var next = DataManager.stat.GetResultStat(_next);

        var compareData = prev.GetCompareResult(next);

        foreach (var d in compareData)
            m_dbStat[d.Key].txtValue.text = $"<color=#BA0700>{d.Value.message}";
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public List<StatData> lstStat;
        public void Initialize(Transform _transform)
        {
            lstStat = new();

            for (var i = StatType.NONE + 1; i < StatType.MAX; i++)
            {
                int idx = (int)i;
                var comp = _transform.GetChild(idx);

                StatData data = new();

                data.type = i;
                data.txtName = comp.GetComponent<TextMeshProUGUI>("txt_name");
                data.txtValue = comp.GetComponent<TextMeshProUGUI>("txt_value");

                lstStat.Add(data);
            }
        }
    }

    [Serializable]
    struct StatData
    {
        public StatType type;
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtValue;
    }

    #endregion VALIDATA
}
