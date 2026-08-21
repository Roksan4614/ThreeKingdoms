using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class UITierPointHelper : UIPowerHelper
{
    protected override void Start()
    {

    }

    public void SetRankInfo(RankerUserData _userData)
    {
        text = _userData.point.AmountKMBT(_isMBT: true);
        SetTierIconAsync(_userData.tierTournament).Forget();
    }

    public async UniTask SetRankInfoAsync(RankerUserData _userData)
    {
        text = _userData.point.AmountKMBT(_isMBT: true);
        await SetTierIconAsync(_userData.tierTournament);
    }

    public async UniTask SetTierIconAsync(int _tier)
    {
        GameObject objTier = null;
        var strTier = _tier.ToString();
        for (int i = 0; i < m_elementTier.tier.childCount; i++)
        {
            if (m_elementTier.tier.GetChild(i).name == strTier)
            {
                objTier = m_elementTier.tier.GetChild(i).gameObject;
                objTier.gameObject.SetActive(true);
            }
            else
                m_elementTier.tier.GetChild(i).gameObject.SetActive(false);
        }

        if (objTier == null)
        {
            var resource = await AddressableManager.instance.GetItemIconAsync("Tier_" + _tier);

            if (resource != null)
            {
                objTier = Instantiate(resource, m_elementTier.tier);
                objTier.name = strTier;
            }
        }
    }

    public GameObject GetTierIcon(int _tier)
    {
        var strTier = _tier.ToString();
        for (int i = 0; i < m_elementTier.tier.childCount; i++)
        {
            if (m_elementTier.tier.GetChild(i).name == strTier)
                return m_elementTier.tier.GetChild(i).gameObject;
        }
        return null;
    }

    #region VALIDATE

    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_elementTier.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_elementTier;

    [System.Serializable]
    struct ElementData
    {
        public Transform tier;

        public void Initialize(Transform _transform)
        {
            tier = _transform.Find("Tier");
        }
    }
    #endregion
}
