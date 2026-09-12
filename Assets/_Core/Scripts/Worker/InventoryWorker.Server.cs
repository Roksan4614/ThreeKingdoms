using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ThreeKingdoms.Client.Server;

public partial class InventoryWorker
{
    public void ApplyServerInventory(IReadOnlyList<ItemData> items)
    {
        m_data = items.Where(item => item != null).Select(item => JsonConvert.DeserializeObject<InventoryItemData>(JsonConvert.SerializeObject(item))).ToList();
        Signal.instance.UpdateAsset.Emit((false, ItemType.NONE));
    }
}
