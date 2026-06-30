using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class HeroIconComponent_UserInfo : HeroIconComponent
{
    private void Awake()
    {
        OnManualValidate();
    }

    public async UniTask SetHeroData_UserInfoAsync(HeroInfoData _heroData)
    {
        SetHeroData(_heroData, null, null);

        m_elementUserInfo.txtPosition.text = TableManager.stringTable.GetHeroPositionType(_heroData.positionType);
        m_elementUserInfo.txtRelicLevel.text = $"Lv.{_heroData.relicLevel}";

        // ICON
        bool isFinded = false;
        var p = m_elementUserInfo.parentIcon;
        for (int i = 0; i < p.childCount; i++)
        {
            var icon = p.GetChild(i);
            icon.gameObject.SetActive(icon.name == _heroData.key);
            if (isFinded == false && icon.gameObject.activeSelf == true)
                isFinded = true;
        }

        if (isFinded == false)
        {
            var prefab = await AddressableManager.instance.GetRelicIconAsync(_heroData.key);

            if (prefab != null)
            {
                var icon = Instantiate(prefab, p);

                var rtParent = icon.transform.parent as RectTransform;
                await UniTask.WaitUntil(() => rtParent.rect.width > 0 || rtParent.rect.height > 0, cancellationToken: destroyCancellationToken);

                icon.AutoResizeParent().name = _heroData.key;
            }
        }
    }

    public override void OnManualValidate()
    {
        base.OnManualValidate();

        m_elementUserInfo.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    ElementData_UserInfo m_elementUserInfo;
    struct ElementData_UserInfo
    {
        public TextMeshProUGUI txtPosition;
        public TextMeshProUGUI txtRelicLevel;

        public Transform parentIcon;

        public void Initialize(Transform _transform)
        {
            txtPosition = _transform.GetComponent<TextMeshProUGUI>("Info/txt_position");
            txtRelicLevel = _transform.GetComponent<TextMeshProUGUI>("Info/Relic/txt_level");
            parentIcon = _transform.Find("Info/Relic/Icon/Panel/Icon/Panel");
        }
    }
}
