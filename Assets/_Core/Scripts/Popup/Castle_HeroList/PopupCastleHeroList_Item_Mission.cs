using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PopupCastleHeroList_Item_Mission : PopupCastleHeroList_Item
{
    public void SetHeroInfoData_Mission(HeroInfoData _heroInfoData, UnityAction<HeroInfoData> _onClick, CoreStatType _coreStatType)
    {
        m_element.button.onClick.RemoveAllListeners();

        m_element.button.onClick.AddListener(() =>
        {
            m_heroInfoData.isBatch = !m_heroInfoData.isBatch;
            m_element.check.SetActive(m_heroInfoData.isBatch);

            _onClick(m_heroInfoData);
        });

        m_heroInfoData = _heroInfoData;

        m_element.heroIcon.SetHeroData(_heroInfoData, null, null);
        //m_element.GetText(TextType.name).text = _heroInfoData.name;

        m_element.check.SetActive(_heroInfoData.isBatch);
        SetCoreStat(_heroInfoData, _coreStatType);

        m_element.bg.SetActive(transform.GetSiblingIndex() % 2 == 1);
    }

    void SetCoreStat(HeroInfoData _heroInfoData, CoreStatType _coreStatType)
    {
        var coreStat = _heroInfoData.resultCoreStat;
        for (int i = 0; i < coreStat.Count; i++)
        {
            CoreStatType coreStatType = (CoreStatType)i;
            TextType txtType = TextType.leadership + i;
            var value = coreStat[coreStatType];
            var txt = m_element.GetText(txtType);

            if (_coreStatType == coreStatType)
                txt.text = $"<color=#{Palette.htmlString_Up}>" + value.ToString();
            else
                txt.text = $"<color=#7e7e7e>{value}";
            txt.alpha = value >= 90 ? 1 : value >= 80 ? .9f : value >= 70 ? .8f : value >= 60 ? .7f : .6f;
        }
    }
}
