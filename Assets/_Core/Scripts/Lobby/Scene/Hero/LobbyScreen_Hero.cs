using UnityEngine;
using UnityEngine.UI;

public partial class LobbyScreen_Hero : LobbyScreen_Base
{
    PopupHeroFilter m_popup;

    protected override void Awake()
    {
        
    }

    private void Start()
    {
        m_element.btn_filter.onClick.AddListener(
            async () =>
            {
                m_popup = await PopupManager.instance.OpenPopupAndWait<PopupHeroFilter>(PopupType.Hero_Filter);
            });
    }

#if UNITY_EDITOR
    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_element.Initialize(transform);
    }
#endif

    [SerializeField, HideInInspector]
    ElementData m_element;
    struct ElementData
    {
        public Button btn_filter;
        public Button btn_mainPosition;

        public void Initialize(Transform _transform)
        {
            btn_filter = _transform.GetComponent<Button>("Panel/Wait/btn_filter");
            btn_mainPosition = _transform.GetComponent<Button>("Panel/Batch/btn_filter");
        }
    }
}
