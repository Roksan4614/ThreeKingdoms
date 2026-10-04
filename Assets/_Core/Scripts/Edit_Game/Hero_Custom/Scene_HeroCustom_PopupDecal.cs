using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Scene_HeroCustom_PopupDecal : MonoBehaviour
{
    public UnityAction<string, bool> actionItem { get; set; }

    List<ButtonHelper> m_buttons = new();
    public List<ButtonHelper> buttons => m_buttons;

    public void Initialize(Transform _parts)
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(() => gameObject.SetActive(false));

        var decal = _parts.Find("Head").GetChild(0).Find("Decal");

        var content = transform.Find("Panel");
        for (int i = 0; i < decal.childCount; i++)
        {
            var btn = (i == content.childCount ? Instantiate(content.GetChild(0), content) : content.GetChild(i))
                .GetComponent<ButtonHelper>();

            btn.name = btn.text = decal.GetChild(i).name;
            var idx = i;
            btn.onClick.AddListener(() =>
            {
                btn.SetDrawSelect(!btn.isDrawSelect);
                actionItem(btn.name, btn.isDrawSelect);
            });

            m_buttons.Add(btn);
        }
    }

    private void Start()
    {
        transform.Find("Panel").ForceRebuildLayout();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            gameObject.SetActive(false);
    }

}
