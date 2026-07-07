using Cysharp.Threading.Tasks;
using UnityEngine;

public class StoryMode_Node1 : StoryModeBaseComponent
{
    protected override void Start()
    {
        base.Start();

        CameraManager.instance.SetCameraPosTarget(mainHero.element.cameraPos);

        ControllerManager.instance.DashTimerStartAsync().Forget();
        ControllerManager.instance.SetActiveButton_StoryMode();
    }

    public async override UniTask StartAsync()
    {
        await UniTask.Yield();
    }
}
