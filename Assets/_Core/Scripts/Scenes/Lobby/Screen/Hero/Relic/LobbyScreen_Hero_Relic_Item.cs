using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyScreen_Hero_Relic_Item : MonoBehaviour, IValidatable
{
    protected HeroInfoData m_heroInfoData;

    bool isRelicTab => m_heroInfoData != null;

    async UniTask OnButtonAsync_Upgrade(UnityAction<HeroInfoData> _onCallback)
    {
        await UniTask.Yield();

        if (isRelicTab)
        {
            m_heroInfoData.relicLevel++;
            DataManager.stat.relic.Upgrade_HeroRelic(m_heroInfoData);
            SetRelicData(m_heroInfoData, true);
        }

        _onCallback(m_heroInfoData);
    }

    protected virtual async UniTask OnButtonAsync_Select(UnityAction<HeroInfoData> _onCallback)
    {
        await UniTask.Yield();

        if (DataManager.stat.relic.dataTreasure.Count(x => x.isBatch == true) >= 3 && m_heroInfoData.isBatch == false)
        {
            PopupManager.instance.AlertShow("최대_3개까지만_장착_가능합니다.");
            return;
        }

        m_heroInfoData.isBatch = !m_heroInfoData.isBatch;
        DataManager.stat.relic.SetTreasureStatus(m_heroInfoData.skin, m_heroInfoData.isBatch);

        m_element.btn_select.SetDrawSelect(m_heroInfoData.isBatch);
        m_element.btn_select.text = m_heroInfoData.isBatch ? "_선택중_" : "선택_하기";

        _onCallback(m_heroInfoData);
    }

    public async UniTask SetTreasureDataAsync(List<Data_Stat_Relic.TreasureBatchData> _dbBatchData, TableTreasureData _treasureData, UnityAction<HeroInfoData> _onClick = null)
    {
        var myTreasureData = _dbBatchData.Where(x => x.key == _treasureData.key).FirstOrDefault();

        m_heroInfoData = new(_treasureData.key);
        m_element.btn_select.interactable = m_heroInfoData.isMine = _treasureData.key.IsActive();
        m_heroInfoData.isBatch = m_heroInfoData.isMine && myTreasureData.isBatch;

        m_element.btn_enchant.gameObject.SetActive(false);
        m_element.btn_select.gameObject.SetActive(true);

        m_element.btn_select.onClick.RemoveAllListeners();
        m_element.btn_select.onClick.AddListener(() =>
        {
            OnButtonAsync_Select(_onClick).Forget();
        });

        m_element.txt_title.text = _treasureData.name;

        m_element.txt_stat.text = "";

        if (m_heroInfoData.isMine == true)
        {
            m_element.txt_stat.text = _treasureData.GetStringEffect();
            m_element.imgPanel.color = myTreasureData.isBatch == true ? Color.gray8 : Color.white;
            m_element.btn_select.text = myTreasureData.isBatch ? "_선택중_" : "선택_하기";
        }
        else
        {
            m_element.txt_stat.text = "?";
            m_element.btn_select.text = "_잠김_";
        }

        m_element.btn_select.SetDrawSelect(myTreasureData.isBatch);

        SetIconAsync("Treasure_" + _treasureData.key, false).Forget();

        await UniTask.Yield();
    }

    public void SetRelicData(HeroInfoData _heroInfoData, bool _isUpdate, UnityAction<HeroInfoData> _onClick = null)
    {
        //능력치 +000.00%\n< size = 80 %> (지휘관 + 000.00 %)
        var statValue = _heroInfoData.relicLevel * 0.01f;
        m_element.txt_stat.text =
            $"기본 능력치_+{(_heroInfoData.relicLevel * 10).AmountKMBT()}%\n<size=80%> ({_heroInfoData.className}_+{(_heroInfoData.relicLevel).AmountKMBT()}%)";

        m_element.txt_level.text = $"Lv.{_heroInfoData.relicLevel}";
        m_element.imgPanel.color = Color.white;

        if (_isUpdate == false)
        {
            m_element.btn_enchant.gameObject.SetActive(true);
            m_element.btn_select.gameObject.SetActive(false);

            m_element.btn_enchant.onClick.RemoveAllListeners();
            m_element.btn_enchant.onClick.AddListener(() =>
            {
                OnButtonAsync_Upgrade(_onClick).Forget();
            });

            m_heroInfoData = _heroInfoData;

            m_element.txt_title.text =
                $"{_heroInfoData.name}:_무기";

            // ICON
            SetIconAsync(_heroInfoData.key, true).Forget();
        }
    }

    async UniTask SetIconAsync(string _key, bool _isRelic)
    {
        bool isFinded = false;
        var p = m_element.parentIcon;
        for (int i = 0; i < p.childCount; i++)
        {
            var icon = p.GetChild(i);
            icon.gameObject.SetActive(icon.name == _key);
            if (isFinded == false && icon.gameObject.activeSelf == true)
                isFinded = true;
        }

        if (isFinded == false)
        {
            var prefab = await AddressableManager.instance.GetIconAsync(_key, _isRelic);

            if (prefab != null)
            {
                var icon = Instantiate(prefab, p);

                var rtParent = icon.transform.parent as RectTransform;
                await UniTask.WaitUntil(() => rtParent.rect.width > 0 || rtParent.rect.height > 0, cancellationToken: destroyCancellationToken);

                icon.AutoResizeParent().name = _key;
            }
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    protected ElementData m_element;

    [Serializable]
    protected struct ElementData
    {
        public Image imgPanel;
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
            imgPanel = panel.GetComponent<Image>();
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
