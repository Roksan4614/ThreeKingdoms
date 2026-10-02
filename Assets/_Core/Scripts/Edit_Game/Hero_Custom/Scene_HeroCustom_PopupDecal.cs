using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Scene_HeroCustom_PopupDecal : MonoBehaviour
{
    Transform m_baseParts;
    public void Initialize(Transform _parts)
    {
        m_baseParts = _parts;
    }

    public UnityAction<int> actionIdx { get; set; }

    public List<ButtonHelper> m_buttons = new();

    private void Start()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(() => gameObject.SetActive(false));
        Utils.WaitEscape(this, () => gameObject.SetActive(false));

        var decal = m_baseParts.Find("Head").GetChild(0).Find("Decal");

        var content = transform.Find("Panel");
        for (int i = 0; i < decal.childCount; i++)
        {
            var btn = (i == content.childCount ? Instantiate(content.GetChild(i), content) : content.GetChild(i))
                .GetComponent<ButtonHelper>();

            btn.name = btn.text = decal.GetChild(i).name;
            var idx = i;
            btn.onClick.AddListener(() => actionIdx(idx));

            m_buttons.Add(btn);
        }
    }


}
