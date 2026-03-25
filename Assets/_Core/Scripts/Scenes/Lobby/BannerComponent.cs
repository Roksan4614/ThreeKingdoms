using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BannerComponent : Singleton<BannerComponent>, IValidatable
{
    protected override void OnAwake()
    {
        m_element.btnBossRaid.onClick.AddListener(() => BossRaidWorker.instance.Initialize(BossRaidWorker.BossRaidType.LuBu).Forget());

        Signal.instance.ActiveHUD.connectLambda = new(this, _isActive => gameObject.SetActive(false));
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public ButtonHelper btnBossRaid;
        public void Initialize(Transform _transform)
        {
            var right = _transform.Find("Right");

            btnBossRaid = right.GetComponent<ButtonHelper>("brn_bossRaid");
        }
    }
    #endregion VALIDATA
}
