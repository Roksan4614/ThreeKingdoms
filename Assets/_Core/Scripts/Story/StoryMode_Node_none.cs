using Cysharp.Threading.Tasks;
using UnityEngine;

public class StoryMode_Node_none : StoryModeBaseComponent
{
    protected override void Start()
    {
        base.Start();
    }

    protected async override UniTask StartAsync()
    {
        CameraManager.instance.SetCameraPosTarget(mainHero.element.cameraPos);

        ControllerManager.instance.DashTimerStartAsync().Forget();
        ControllerManager.instance.SetActiveButton_StoryMode();
        ControllerManager.instance.SetSwitch(true);

        await UniTask.Yield();
    }
}
