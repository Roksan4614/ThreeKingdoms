using Cysharp.Threading.Tasks;
using UnityEngine;

public class TreasureIconComponent : ItemComponent
{
    TooltipWorker m_tooltip;

    protected override void Awake()
    {
        base.Awake();

        m_tooltip = transform.GetComponent<TooltipWorker>("Tooltip");
    }

    public async UniTask SetTreasureDataAsync(string _key)
    {
        m_tooltip.text = TableManager.treasure.Get(_key).GetStringEffect(false);

        gameObject.SetActive(true);

        m_element.icon.SetActive(_key.IsActive());
        m_element.empty.SetActive(_key.IsActive() == false);

        if (_key.IsActive() == false)
            return;

        bool isFinded = false;
        for (int i = 0; i < m_element.iconPanel.childCount; i++)
        {
            var icon = m_element.iconPanel.GetChild(i).gameObject;

            icon.SetActive(icon.name.Equals(_key));
            if (isFinded == false)
                isFinded = icon.activeSelf;
        }

        if (isFinded == false && _key.IsActive())
        {
            var result = await AddressableManager.instance.GetIconAsync($"Treasure_{_key}", false);
            if (result == null)
                return;

            var icon = Instantiate(result, m_element.iconPanel);
            icon.AutoResizeParent().name = _key;
        }

    }
}
