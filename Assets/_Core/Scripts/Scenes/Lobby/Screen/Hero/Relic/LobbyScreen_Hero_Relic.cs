using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Hero_Relic : LobbyScreen_Hero_TabBase, IValidatable
{
    enum TapType
    {
        Character, Relic,
    }

    void UpdateTotalClass()
    {

    }

    void UpdateTotalStat()
    {

    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public TextMeshProUGUI[] txtTotalClass;
        public TotalStatData baseTotalStat;

        public Transform pTotalClass => txtTotalClass[0].transform.parent;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");
            txtTotalClass = panel.Find("Total_Class").GetComponentsInChildren<TextMeshProUGUI>(true);

            baseTotalStat = new();
            baseTotalStat.txtName = panel.Find("Total_Stat/Text").GetComponent<TextMeshProUGUI>();
            baseTotalStat.txtValue = panel.Find("Total_Stat/Text/Text").GetComponent<TextMeshProUGUI>();
        }
    }

    [Serializable]
    struct TotalStatData
    {
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtValue;
    }
    #endregion VALIDATA
}
