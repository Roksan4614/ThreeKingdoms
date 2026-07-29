using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class PopupUpgradeGuideComponent : BasePopupComponent, IValidatable
{
    PopupUpgradeGuideComponent() : base(PopupType.UpgradeGuide) { }

    private void Start()
    {
        var parent = transform.Find("Panel");
        var baseButton = parent.Find("Button");

        int idxStart = baseButton.GetSiblingIndex();
        int max = (int)UpgradeGuideType.MAX;
        for (var i = 0; i < max; i++)
        {
            var idx = idxStart + i;
            var type = (UpgradeGuideType)i;

            var button = (idx == parent.childCount ? Instantiate(baseButton, parent) : parent.GetChild(idx)).GetComponent<ButtonHelper>();
            button.onClick.AddListener(() => OnButton(type));

            button.text = TableManager.stringTable.GetString($"UPGRADE_GUIDE_{type}_TITLE");
            button.text += "\n<color=#6f6f6f><size=55%>" + TableManager.stringTable.GetString($"UPGRADE_GUIDE_{type}_DESC") + "</size></color>";
        }
    }

    bool m_isDoing = false;
    void OnButton(UpgradeGuideType _type)
    {
        if (m_isDoing == true)
            return;

        m_isDoing = true;

        switch (_type)
        {
            case UpgradeGuideType.SCREEN_HERO:
                LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Hero);
                break;
            case UpgradeGuideType.CASTLE:
                LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Castle);
                break;
            case UpgradeGuideType.GACHA:
                LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Summon);
                break;
            case UpgradeGuideType.TIMELOOP:
                TimeLoopAsync().Forget();
                break;
        }

        IngameLog.Add(_type);

        Close();
    }

    async UniTask TimeLoopAsync()
    {
        await UniTask.Yield();
    }

    public override void Close()
        => Utils.SetActivePunch(transform.Find("Panel"), false, true, base.Close);

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;
        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/Result/Text");
        }
    }
    #endregion VALIDATE

    public enum UpgradeGuideType
    {
        None = -1,

        SCREEN_HERO,
        CASTLE,
        GACHA,
        TIMELOOP,

        MAX
    }
}
