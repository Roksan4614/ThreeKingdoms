using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyScreen_Boss_Tab_Slot : MonoBehaviour, IValidatable
{
    public void SetDungeonData(WeekdayType _weekday, TableDailyDungeonBossData _bossData, UnityAction<TableDailyDungeonBossData> _callback)
    {
        m_element.button.onClick.RemoveAllListeners();
        m_element.button.onClick.AddListener(() => _callback(_bossData));

        m_element.txtName.text = $"<size=70%>{TableManager.stringTable.GetString($"WEEKDAY_{_bossData.weekday.ToString().ToUpper()}_FULL")}</size>\r\n{_bossData.name}";

#if UNITY_EDITOR
        bool isActive = true;
#else
        bool isActive = _weekday == WeekdayType.Sunday || _bossData.weekday == _weekday;
#endif

        m_element.dimm.SetActive(isActive == false);

        for (int i = 0; i < m_element.icons.Length; i++)
            m_element.icons[i].gameObject.SetActive(i == (int)_bossData.weekday - 1);

        m_element.button.interactable = isActive;
    }
#if SERVICE_DEV

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            m_element.dimm.SetActive(m_element.dimm.activeSelf == false);
            m_element.button.interactable = m_element.dimm.activeSelf == false;
        }
    }
#endif

    public void SetSelect(bool _isSelect)
        => m_element.objSelect.SetActive(_isSelect);

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Button button;

        public TextMeshProUGUI txtName;
        public GameObject dimm;
        public GameObject objSelect;

        public GameObject[] icons;

        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<Button>();
            txtName = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_name");
            dimm = _transform.Find("Panel/Dimm").gameObject;
            objSelect = _transform.Find("Select").gameObject;

            List<GameObject> lstIcons = new();
            var pIcon = _transform.Find("Panel/Icon");
            for (int i = 0; i < pIcon.childCount; i++)
                lstIcons.Add(pIcon.GetChild(i).gameObject);
            icons = lstIcons.ToArray();
        }
    }
    #endregion VALIDATE

}
