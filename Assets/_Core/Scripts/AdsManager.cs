using Cysharp.Threading.Tasks;
using UnityEngine;

public class AdsManager
{
    public static AdsManager instance { get; private set; } = new();
    public void Release() => instance = null;

    public async UniTask<bool> ShowAsync()
    {
        PopupManager.instance.ShowDimm(true, false);

        await UniTask.WaitForSeconds(1f);

        PopupManager.instance.AlertShow("광고 시청 완료");

        PopupManager.instance.ShowDimm(false);

        return true;
    }
}
