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
        PopupManager.instance.ShowDimm(false);

        CameraManager.instance.SetCameraPosTarget(mainHero.cameraPos);

        ControllerManager.instance.DashTimerStartAsync().Forget();
        ControllerManager.instance.SetActiveButton_StoryMode(true);
        ControllerManager.instance.SetSwitch(true);

        await UniTask.WaitUntil(() => Input.GetKey(KeyCode.Escape));
    }
}
