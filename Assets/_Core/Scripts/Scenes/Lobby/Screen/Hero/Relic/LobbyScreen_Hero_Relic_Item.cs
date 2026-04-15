using UnityEngine;
using UnityEngine.Events;

public class LobbyScreen_Hero_Relic_Item : MonoBehaviour
{
    RelicInfoData m_relicData;

    public void Bind(UnityAction<RelicInfoData> _onCallback)
    {

    }

    public void SetHeroData(RelicInfoData _data)
    {
        m_relicData = _data;
    }
}
