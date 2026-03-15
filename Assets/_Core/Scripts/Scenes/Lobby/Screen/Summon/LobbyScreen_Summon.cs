using System;
using UnityEngine;

public class LobbyScreen_Summon : LobbyScreen_Base
{
    LobbyScreen_Summon_Package package => m_element.package;

    protected override void Awake()
    {
        base.Awake();

        package.SetActive(true);
    }

    public void SetEnableRegion(params RegionType[] _region)
        => package.SetEnableRegion(_region);

    #region VALIDATE
    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_element.Initialize(transform);
    }

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        [SerializeField] LobbyScreen_Summon_Package m_package;
        public LobbyScreen_Summon_Package package => m_package;

        public void Initialize(Transform _transform)
        {
            m_package = _transform.GetComponent<LobbyScreen_Summon_Package>("Panel/BG/Content/Package");
        }
    }
    #endregion VALIDATA
}
