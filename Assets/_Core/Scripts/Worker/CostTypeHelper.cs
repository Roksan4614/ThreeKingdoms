using ThreeKingdoms.Shared.Enums;
using UnityEngine;

public class CostTypeHelper : MonoBehaviour
{
    public void SetCostType(PayType _payType)
    {
        string payType = _payType.ToString();
        for (int i = 0; i < transform.childCount; i++)
        {
            var obj = transform.GetChild(i).gameObject;
            obj.SetActive(obj.name.Equals(payType));
        }
    }
}
