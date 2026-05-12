using Cysharp.Threading.Tasks;
using UnityEngine;
using CastleMissionData = Data_Castle_Mission.CastleMissionData;

public class PopupCastleMission_Popup_Info : MonoBehaviour
{
    public async UniTask<bool> OpenAsync(CastleMissionData _mission)
    {
        return true;
    }
}
