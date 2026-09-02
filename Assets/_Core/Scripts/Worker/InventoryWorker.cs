using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class InventoryWorker
{
    static InventoryWorker m_instance;
    public static InventoryWorker instance
    {
        get
        {
            if (m_instance == null)
                m_instance = new();
            return m_instance;
        }
    }
    public void Release()
    {
        m_instance = null;
    }

    List<InventoryItemData> m_data;
    const string c_key = "pp_inventory";

    void SaveData() => PPWorker.Set(c_key, m_data);

    public async UniTask InitializeAsync()
    {
        m_data = PPWorker.Get<List<InventoryItemData>>(c_key);

        if (m_data == null)
        {
            m_data = new List<InventoryItemData>();
            SaveData();
        }

        await UniTask.NextFrame();
    }

    public long GetItemCount(ItemData _itemData)
        => m_data.Find(x => x.key == _itemData.key && x.value == _itemData.value)?.count ?? 0;
}

public class ItemData : TableItemData
{
    //custom 
    public bool isNew;
    public long count;
}

[JsonObject(MemberSerialization.OptIn)]
public class InventoryItemData : ItemData
{
}
