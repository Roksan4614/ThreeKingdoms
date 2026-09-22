using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupSetting_Language : MonoBehaviour
{
    RectTransform m_rtSelect;

    void Start()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(Close);

        m_rtSelect = (RectTransform)transform.Find("Panel/Select");
        m_rtSelect.gameObject.SetActive(true);

        var panel = m_rtSelect.parent;
        var stringData = TableManager.stringTable.Get("UI_SET_LANGUAGE");
        var baseSlot = panel.Find("Text");
        RectTransform rtCur = null;
        for (var i = LanguageType.None + 1; i < LanguageType.Max; i++)
        {
            var language = i;
            var idx = (int)i;
            var txt = (idx == 0 ? baseSlot : Instantiate(baseSlot, panel)).GetComponent<TextMeshProUGUI>();
            var button = txt.transform.GetComponent<Button>();
            txt.text = stringData.GetMessage(language);
            button.onClick.AddListener(() => OnButton(language));

            if (DataManager.option.language == language)
            {
                rtCur = txt.rectTransform;
                txt.color = Color.white;
            }
            else
                txt.color = Color.gray1;
        }

        panel.ForceRebuildLayout();

        m_rtSelect.SetAnchoredPositionY(rtCur.anchoredPosition.y);
    }

    private void OnEnable()
    {
        m_isDoing = false;
    }

    bool m_isDoing = false;
    void OnButton(LanguageType _language)
    {
        if (m_isDoing == true)
            return;

        m_isDoing = true;
        if (DataManager.option.language != _language)
        {
            DataManager.option.language = _language;
            LoadSceneWorker.instance.RestartApp(false);
        }
        else
            Close();
    }

    void Close()
        => gameObject.SetActive(false);
}
