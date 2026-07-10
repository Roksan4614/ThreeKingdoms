using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class Scene_StoryMode : SceneBase
{
    private void Start()
    {
        InfoStageComponent.instance.gameObject.SetActive(false);

        StartAsync().Forget();
    }

    async UniTask StartAsync()
    {
        await UniTask.NextFrame();

        List<UniTask> tasks = new();
        tasks.Add(StageManager.instance.InitializeAsync_StoryMode());

        await UniTask.WhenAll(tasks);

        PopupManager.instance.ShowDimm(false);

        isReady = true;
    }
}
