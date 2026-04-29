using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Hero_Collection_Item : MonoBehaviour, IValidatable
{
    HeroIconComponent m_baseIcon;

    private void Awake()
    {
        m_baseIcon = m_element.icons.GetChild(0).GetComponent<HeroIconComponent>();
        m_baseIcon.gameObject.SetActive(false);
        m_baseIcon.transform.SetParent(transform);

    }

    public void SetData(TableFriendShipData _data)
    {
        gameObject.SetActive(true);

        m_element.txtTitle.text = _data.title;

        var parent = m_element.icons;
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


    }

    async UniTask OnButtonAsync_Hero(HeroInfoData _heroInfoData)
    {
        var popup = await PopupManager.instance.OpenPopup<PopupHeroInfo>(PopupType.Hero_HeroInfo);

        popup.SetHeroInfoDataAsync(_heroInfoData, DataManager.userInfo.GetHeroInfoData(_heroInfoData.key).isMine == false).Forget();
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
        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");

            txtTitle = panel.GetComponent<TextMeshProUGUI>("txt_title");
            icons = panel.Find("Icons");
        }
    }
    #endregion VALIDATE
}
