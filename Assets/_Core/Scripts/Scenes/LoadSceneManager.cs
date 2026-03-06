using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneManager : MonoSingleton<LoadSceneManager>
{
    public void RestartApp()
    {
        Signal.instance.RestartApp.Emit();
        SceneManager.LoadScene("00_Boot");
    }
}
