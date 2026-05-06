using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Hero_Collection_Item : MonoBehaviour, IValidatable
{
    HeroIconComponent m_baseIcon;

    public void SetData(TableFriendShipData _data)
    {
        gameObject.SetActive(true);

        // TITLE
        m_element.txtTitle.text = $"Lv.{(int)_data.minGrade + 1} {_data.title}"; ;

        // ICON
        var parent = m_element.icons;

        if (m_baseIcon == null)
        {
            m_baseIcon = m_element.icons.GetChild(0).GetComponent<HeroIconComponent>();
            m_baseIcon.gameObject.SetActive(false);
            m_baseIcon.transform.SetParent(transform);
        }

        int i = 0;
        for (; i < _data.splitHero.Length; i++)
        {
            bool isNew = parent.childCount == i;

            var item = isNew ? Instantiate(m_baseIcon, parent) : parent.GetChild(i).GetComponent<HeroIconComponent>();

            HeroInfoData heroInfoData = new(_data.splitHero[i], _data.grade[i]);
            heroInfoData.isMine = DataManager.userInfo.GetHeroInfoData(heroInfoData.key).isMine;

            item.SetHeroData(heroInfoData, (_icon, _) => OnButtonAsync_Hero(_icon.data).Forget(), null);
            item.gameObject.SetActive(true);
        }

        for (; i < parent.childCount; i++)
            parent.GetChild(i).gameObject.SetActive(false);

        // ATTRIBUTE
        var stats = transform.Find("Panel/Stats");

        for (i = 0; i < _data.statData.Count; i++)
        {
            if (i == stats.childCount)
                Instantiate(stats.GetChild(0), stats);

            var statData = _data.statData[i];
            statData.value = statData.value + statData.value * ((int)_data.minGrade * 0.1f);
            stats.GetChild(i).GetComponent<TextMeshProUGUI>().text
                = statData.statName + $" <color=#BA0700>{statData.stringPercent}";
        }

        stats.ForceRebuildLayout();
    }

    async UniTask OnButtonAsync_Hero(HeroInfoData _heroInfoData)
    {
        var popup = await PopupManager.instance.OpenPopup<PopupHeroInfo>(PopupType.Hero_HeroInfo);

        _heroInfoData = DataManager.userInfo.GetHeroInfoData(_heroInfoData.key);

        popup.SetHeroInfoDataAsync(_heroInfoData, _heroInfoData.isMine == false).Forget();
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform icons;
        public TextMeshProUGUI txtTitle;

        //public Transform trnsBaseItem;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");

            txtTitle = panel.GetComponent<TextMeshProUGUI>("txt_title");
            icons = panel.Find("Icons");

            //trnsBaseItem = icons.GetChild(0);
        }
    }
    #endregion VALIDATE
}
