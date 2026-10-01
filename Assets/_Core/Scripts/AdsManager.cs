using Cysharp.Threading.Tasks;
using UnityEngine;

public class AdsManager : BaseWorker<AdsManager>
{

    public async UniTask<bool> ShowAsync()
    {
        PopupManager.instance.ShowDimm(true, false);

        await UniTask.WaitForSeconds(1f);

        PopupManager.instance.AlertShow_Table("AD_COMPLETE");

        PopupManager.instance.ShowDimm(false);

        return true;
    }
}
