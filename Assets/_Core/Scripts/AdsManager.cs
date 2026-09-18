using Cysharp.Threading.Tasks;
using UnityEngine;

public class AdsManager
{
    static AdsManager m_instance;
    public static AdsManager instance => m_instance ??= new();
    public static void Release() => m_instance = null;

    public async UniTask<bool> ShowAsync()
    {
        PopupManager.instance.ShowDimm(true, false);

        await UniTask.WaitForSeconds(1f);

        PopupManager.instance.AlertShow_Table("AD_COMPLETE");

        PopupManager.instance.ShowDimm(false);

        return true;
    }
}
