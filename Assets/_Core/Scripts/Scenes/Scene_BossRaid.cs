using Cysharp.Threading.Tasks;
using UnityEngine;

public class Scene_BossRaid : SceneBase
{
    bool m_isExit;
    private void Start()
    {
        PopupManager.instance.ShowDimm(false);
    }

    private void Update()
    {
        if (m_isExit == false && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitAsync().Forget();
        }
    }

    async UniTask ExitAsync()
    {
        m_isExit = true;
        await PopupManager.instance.ShowDimmAsync(true, false);
        AddressableManager.instance.LoadScene("02_Lobby");
    }
}
