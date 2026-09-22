using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneWorker
{
    static LoadSceneWorker m_instance;
    public static LoadSceneWorker instance => m_instance ??= new();

    public bool isRestart { get; private set; }

    public void RestartApp(bool _isLogout)
        => RestartAppAsync(_isLogout).Forget();

    async UniTask RestartAppAsync(bool _isLogout)
    {
        await PopupManager.instance.ShowDimmAsync(true);

        isRestart = true;
        if(_isLogout == true)
        {
            AuthWorker.Release();
            DataManager.Release();
        }

        Signal.instance.RestartApp.Emit();
        AddressableManager.instance.LoadScene("01_Login");
    }
}
