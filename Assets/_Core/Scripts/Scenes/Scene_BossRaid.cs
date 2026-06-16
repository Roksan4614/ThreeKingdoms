using Cysharp.Threading.Tasks;
using UnityEngine;

public class Scene_BossRaid : SceneBase
{
    bool m_isExit;
    private void Start()
    {
        StartAsync().Forget();
    }

    async UniTask StartAsync()
    {
        await UniTask.WaitForEndOfFrame();
        PopupManager.instance.ShowDimm(false);
    }

    private void Update()
    {
        if (m_isExit == false && Input.GetKeyDown(KeyCode.Escape))
        {
            m_isExit = true;
            BossRaidWorker.instance.FinishedAsync().Forget();
        }
    }
}
