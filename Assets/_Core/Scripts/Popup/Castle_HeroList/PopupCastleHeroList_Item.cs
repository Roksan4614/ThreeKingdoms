using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupCastleHeroList_Item : MonoBehaviour, IValidatable
{
    protected enum TextType
    {
        leadership,
        strength,
        intellect,
        politics,
        charisma,
        job,
        batch,
    }

    protected HeroInfoData m_heroInfoData;
    public HeroInfoData heroInfoData => m_heroInfoData;

    Data_Castle.CastleData m_castleData;
    CastleObjectType m_prevJob;

    public void SetHeroInfoData(Data_Castle.CastleData _castleData, HeroInfoData _heroInfoData, UnityAction<HeroInfoData> _onClick, params CoreStatType[] _coreStatType)
    {
        m_castleData = _castleData;

        m_element.button.onClick.RemoveAllListeners();
        m_element.button.onClick.AddListener(() =>
        {
            var job = m_heroInfoData.isBatch == true ? m_prevJob : m_castleData.type;
            if (m_prevJob > CastleObjectType.NONE)
            {
                // 이전 임무와 다르다면..
                if (m_prevJob != m_castleData.type)
                {
                    // 그런데 배치를 시도했네? 확인 팝업 띄우자
                    if (m_heroInfoData.isBatch == false)
                    {
                        PopupManager.instance.OpenModalAsync(
                            "이미_임무_중인_장수입니다.\n새로운_임무를_부여하겠습니까?", _callback: _statusType =>
                            {
                                if (_statusType == StatusType.Success)
                                {
                                    m_heroInfoData.isBatch = !m_heroInfoData.isBatch;
                                    m_element.check.SetActive(m_heroInfoData.isBatch);

                                    _onClick(m_heroInfoData);
                                    SetJob(job);
                                }
                            }
                            ).Forget();
                        return;
                    }
                }
                //이전임무와 같은데 해제하는거라면
                else if (m_heroInfoData.isBatch == true)
                    job = CastleObjectType.NONE;
            }

            m_heroInfoData.isBatch = !m_heroInfoData.isBatch;
            m_element.check.SetActive(m_heroInfoData.isBatch);

            _onClick(m_heroInfoData);
            SetJob(job);
        });

        m_heroInfoData = _heroInfoData;

        m_element.heroIcon.SetHeroData(_heroInfoData, null, null, true);
        //m_element.GetText(TextType.name).text = _heroInfoData.name;

        m_element.check.SetActive(_heroInfoData.isBatch);
        SetCoreStat(_heroInfoData, _coreStatType);

        m_prevJob = DataManager.castle.GetHeroObjectType(m_heroInfoData.key);
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
                txt.text = $"<color=#{Palette.htmlString_Up}>" + value.ToString();
            else
                txt.text = $"<color=#7e7e7e>{value}";
            txt.alpha = value >= 90 ? 1 : value >= 80 ? .9f : value >= 70 ? .8f : value >= 60 ? .7f : .6f;
        }
    }

    public void SetActive_Batch(bool _isShow, string _stringBatch = "")
    {
        var txt = m_element.GetText(TextType.batch);
        txt.gameObject.SetActive(_isShow == false);

        if (_stringBatch.IsActive())
            txt.text = _stringBatch;
    }

    public Button.ButtonClickedEvent onClick_HeroIcon => m_element.heroIcon.onClick;

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    protected ElementData m_element;

    [Serializable]
    protected struct ElementData
    {
        public Transform panel;
        public Button button;

        public GameObject check;
        public TextMeshProUGUI txtBatch;

        public GameObject bg;

        [SerializeField] TextMeshProUGUI[] txt;

        public HeroIconComponent heroIcon;

        public TextMeshProUGUI GetText(TextType _type)
            => txt[(int)_type];

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
            button = _transform.GetComponent<Button>();

            bg = _transform.Find("BG").gameObject;
            check = panel.Find("Batch/CheckBox/Check").gameObject;
            txtBatch = panel.GetComponent<TextMeshProUGUI>("Batch/Text");

            List<TextMeshProUGUI> lstTxt = new();
            for (int i = 0; i < panel.childCount; i++)
            {
                var txtItem = panel.GetChild(i).GetComponent<TextMeshProUGUI>();
                if (txtItem)
                    lstTxt.Add(txtItem);
            }
            txt = lstTxt.ToArray();

            heroIcon = _transform.GetComponent<HeroIconComponent>("Panel/Icon/HeroIcon");
        }
    }
    #endregion VALIDATE
}
