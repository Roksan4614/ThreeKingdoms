using Cysharp.Threading.Tasks;
using UnityEngine;

public class Scene_Login : SceneBase
{
    private void Start()
    {
        PopupManager.instance.ShowDimm(true, false);
        PopupManager.instance.SetCanvasCamera();

        StartAsync().Forget();
    }

    async UniTask StartAsync()
    {
        await UniTask.WaitForEndOfFrame();

        //await PopupManager.instance.ShowDimmAsync(false);

        //await PopupManager.instance.ShowDimmAsync(true);

        AddressableManager.instance.LoadScene("02_Lobby");
    }
}
