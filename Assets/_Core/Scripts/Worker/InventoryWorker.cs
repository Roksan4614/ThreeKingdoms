using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
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

    public async UniTask InitializeAsync()
    {
        await UniTask.NextFrame();
    }

}

public class ItemData: TableItemData
{
    //custom 
    public bool isNew;
    public long count;
}

[JsonObject(MemberSerialization.OptIn)]
public class InventoryItemData : ItemData
{
}
