using System;
using UnityEngine;

public class BannerComponent : Singleton<BannerComponent>, IValidatable
{
    protected override void OnAwake()
    {
        Signal.instance.ActiveHUD.connectLambda = new(this, _isActive =>
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(_isActive);
        });
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public void Initialize(Transform _transform)
        {
        }
    }
    #endregion VALIDATA
}
