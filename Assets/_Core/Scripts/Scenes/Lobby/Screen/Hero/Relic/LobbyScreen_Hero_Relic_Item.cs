using Cysharp.Threading.Tasks;
using System;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyScreen_Hero_Relic_Item : MonoBehaviour, IValidatable
{
    HeroInfoData m_heroInfoData;

    bool isHeroTab => m_heroInfoData.isActive;

    public void Bind(UnityAction<HeroInfoData> _onCallback)
    {
        m_element.btn_enchant.onClick.AddListener(() =>
        {
            OnButtonAsync_Upgrade(_onCallback).Forget();
        });

        m_element.btn_select.onClick.AddListener(() =>
        {
            OnButtonAsync_Select(_onCallback).Forget();
        });
    }

    async UniTask OnButtonAsync_Upgrade(UnityAction<HeroInfoData> _onCallback)
    {
        await UniTask.Yield();

        if (isHeroTab)
        {
            m_heroInfoData.enchantLevel++;
            DataManager.stat.relic.Upgrade_HeroRelic(m_heroInfoData);
            SetHeroDataAsync(m_heroInfoData, true).Forget();
        }

        _onCallback(m_heroInfoData);
    }

    async UniTask OnButtonAsync_Select(UnityAction<HeroInfoData> _onCallback)
    {
        await UniTask.Yield();

        _onCallback(m_heroInfoData);
    }

    public async UniTask SetRelicDataAsync(string _key)
    {
        m_heroInfoData = new();
        m_heroInfoData.key = _key;

        m_element.btn_enchant.gameObject.SetActive(false);
        m_element.btn_select.gameObject.SetActive(true);

        m_element.txt_title.text = $"{_key}";

        await UniTask.Yield();
    }

    public async UniTask SetHeroDataAsync(HeroInfoData _heroInfoData, bool _isUpdate = false)
    {
        //능력치 +000.00%\n< size = 80 %> (지휘관 + 000.00 %)
        var statValue = _heroInfoData.enchantLevel * 0.01f;
        m_element.txt_stat.text =
            $"기본 능력치_+{(_heroInfoData.enchantLevel * 10).AmountKMBT()}%\n<size=80%> ({_heroInfoData.className}_+{(_heroInfoData.enchantLevel * 0.1f).AmountKMBT()}%)";

        m_element.txt_level.text = $"Lv.{_heroInfoData.enchantLevel}";

        if (_isUpdate == false)
        {
            m_element.btn_enchant.gameObject.SetActive(true);
            m_element.btn_select.gameObject.SetActive(false);

            m_heroInfoData = _heroInfoData;

            m_element.txt_title.text =
                $"{_heroInfoData.name}:_무기";

            // ICON
            bool isFinded = false;
            var p = m_element.parentIcon;
            for (int i = 0; i < p.childCount; i++)
            {
                var icon = p.GetChild(i);
                icon.gameObject.SetActive(icon.name == _heroInfoData.key);
                if (isFinded == false && icon.gameObject.activeSelf == true)
                    isFinded = true;
            }

            if (isFinded == false)
            {
                // TODO: 무기 아이콘으로 해야 해
                var prefab = await AddressableManager.instance.GetHeroIconAsync(_heroInfoData.key)
                    .AttachExternalCancellation(destroyCancellationToken);

                if (prefab != null)
                {
                    var icon = Instantiate(prefab, p);

                    var rtParent = icon.transform.parent as RectTransform;
                    await UniTask.WaitUntil(() => rtParent.rect.width > 0 || rtParent.rect.height > 0);

                    icon.AutoResizeParent().name = _heroInfoData.key;
                }
            }
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform parentIcon;

        public TextMeshProUGUI txt_title;
        public TextMeshProUGUI txt_stat;

        public ButtonHelper btn_select;

        public ButtonHelper btn_enchant;
        public TextMeshProUGUI txt_amount;
        public TextMeshProUGUI txt_level;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");
            parentIcon = panel.Find("Icon/Panel");

            txt_title = panel.GetComponent<TextMeshProUGUI>("txt_title");
            txt_stat = panel.GetComponent<TextMeshProUGUI>("txt_stat");

            btn_select = panel.GetComponent<ButtonHelper>("btn_select");

            btn_enchant = panel.GetComponent<ButtonHelper>("btn_enchant");
            txt_amount = btn_enchant.transform.GetComponent<TextMeshProUGUI>("Amount/Text");
            txt_level = btn_enchant.transform.GetComponent<TextMeshProUGUI>("txt_level");
        }
    }
    #endregion VALIDATA
}
