using UnityEngine;

public class LobbyScreen_Castle_Popup_Setting_Palace : MonoBehaviour, IValidatable
{
    private void Start()
    {
        m_element.market.textTitle = "±ÝÈ­_È¹µæ·®";
        m_element.farm.textTitle = "±º·®_È¹µæ·®";

        m_element.market.fillAmount = m_element.farm.fillAmount = 1;
    }

    private void OnEnable()
    {
        m_element.market.textAmount = DataManager.castle.GetCaslteData(CastleObjectType.Market).todayClaimAmount.ToString("#,0");
        m_element.farm.textAmount = DataManager.castle.GetCaslteData(CastleObjectType.Farm).todayClaimAmount.ToString("#,0");
    }


    //protected virtual void OnEnable()
    //{
    //    SlotUpdateFarmMarketData(DataManager.castle.GetCaslteData(CastleObjectType.Market));
    //    SlotUpdateFarmMarketData(DataManager.castle.GetCaslteData(CastleObjectType.Farm));
    //}

    //void SlotUpdateFarmMarketData(Data_Castle.CastleData _castleData)
    //{
    //    if (gameObject.activeInHierarchy == false)
    //        return;

    //    var gauge = _castleData.type == CastleObjectType.Market ? m_element.market : m_element.farm;

    //    var maxAmount = DataManager.castle.GetMaxAmount(_castleData);

    //    gauge.textAmount = $"{Mathf.RoundToInt(_castleData.totalAmount):#,0}/{maxAmount:#,0}";
    //    gauge.fillAmount = _castleData.totalAmount / (float)maxAmount;
    //}

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    protected ElementData m_element;

    [System.Serializable]
    protected struct ElementData
    {
        public GaugeHelper[] gauge;
        public GaugeHelper market => gauge[0];
        public GaugeHelper farm => gauge[1];

        public void Initialize(Transform _transform)
        {
            gauge = _transform.GetComponentsInChildren<GaugeHelper>();
        }
    }
    #endregion VALIDATE

}
