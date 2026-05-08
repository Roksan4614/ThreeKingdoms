using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupCastleHeroList_Item : MonoBehaviour, IValidatable
{
    enum TextType
    {
        name,
        leadership,
        strength,
        intellect,
        politics,
        charisma,
        job,
    }

    HeroInfoData m_heroInfoData;
    public HeroInfoData heroInfoData => m_heroInfoData;

    Data_Castle.CastleData m_castleData;
    CastleObjectType m_prevJob;

    public void SetHeroInfoData(Data_Castle.CastleData _castleData, HeroInfoData _heroInfoData, UnityAction<HeroInfoData> _onClick, params CoreStatType[] _coreStatType)
    {
        m_castleData = _castleData;

        m_element.button.onClick.RemoveAllListeners();
        m_element.button.onClick.AddListener(() =>
        {
            m_heroInfoData.isBatch = !m_heroInfoData.isBatch;
            m_element.check.SetActive(m_heroInfoData.isBatch);
            _onClick(m_heroInfoData);

            var job = m_heroInfoData.isBatch == false ? m_prevJob : m_castleData.type;
            if (m_prevJob > CastleObjectType.NONE)
            {
                if (m_prevJob != m_castleData.type)
                {
                    var prevCastleData = DataManager.castle.GetCaslteData(m_prevJob);

                    if (m_heroInfoData.isBatch == true)
                        prevCastleData.heroes.Remove(m_heroInfoData.key);
                    else
                        prevCastleData.heroes.Add(m_heroInfoData.key);

                    DataManager.castle.UpdateCastleData(prevCastleData, false);
                }
                else if (m_heroInfoData.isBatch == false)
                    job = CastleObjectType.NONE;
            }

            SetJob(job);
        });

        m_heroInfoData = _heroInfoData;

        m_element.GetText(TextType.name).text = _heroInfoData.name;

        m_element.GetText(TextType.job).text = "-";
        m_element.check.SetActive(_heroInfoData.isBatch);

        SetCoreStat(_heroInfoData, _coreStatType);

        m_prevJob = DataManager.castle.GetJobObjectType(m_heroInfoData.key);
        SetJob(m_prevJob);

        m_element.bg.SetActive(transform.GetSiblingIndex() % 2 == 1);
    }

    void SetJob(CastleObjectType _castleObjectType)
    {
        var name = _castleObjectType == CastleObjectType.NONE ? "-" : DataManager.castle.GetObjectName(_castleObjectType);

        if (_castleObjectType == m_castleData.type)
            name = $"<color=#{Palette.htmlString_Up}>{name}";

        m_element.GetText(TextType.job).text = name;
    }


    void SetCoreStat(HeroInfoData _heroInfoData, CoreStatType[] _coreStatType)
    {
        TableCastleRiseData dbRise = TableManager.castleRise.GetRiseData(m_castleData.type, m_castleData.level);

        var coreStat = _heroInfoData.resultCoreStat;
        for (int i = 0; i < coreStat.Count; i++)
        {
            CoreStatType coreStatType = (CoreStatType)i;
            TextType txtType = TextType.leadership + i;
            var value = coreStat[coreStatType];
            var txt = m_element.GetText(txtType);

            if (_coreStatType.Length > 0 && _coreStatType.Contains(coreStatType))
                txt.text = $"<color=#{(dbRise.maxCoreStat[_coreStatType[0] == coreStatType ? 0 : 1] <= value ? Palette.htmlString_Up : Palette.htmlString_Down)}>" + value.ToString();
            else
                txt.text = value.ToString();

            txt.alpha = value >= 90 ? 1 : value >= 80 ? .9f : value >= 70 ? .8f : value >= 60 ? .7f : .6f;
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform panel;
        public Button button;
        public GameObject check;
        public GameObject bg;

        [SerializeField] TextMeshProUGUI[] txt;

        public TextMeshProUGUI GetText(TextType _type)
            => txt[(int)_type];

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
            button = _transform.GetComponent<Button>();

            check = panel.Find("txt_batch/CheckBox/Check").gameObject;
            bg = _transform.Find("BG").gameObject;

            txt = panel.GetComponentsInChildren<TextMeshProUGUI>();
        }
    }
    #endregion VALIDATE
}
