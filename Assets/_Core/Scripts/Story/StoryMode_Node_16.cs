using Cysharp.Threading.Tasks;
using UnityEngine;

public class StoryMode_Node_16 : StoryModeBaseComponent
{
    protected override async UniTask StartAsync()
    {
        CameraManager.instance.SetCameraPosTarget(mainHero.element.cameraPos);

        ControllerManager.instance.DashTimerStartAsync().Forget();
        ControllerManager.instance.SetActiveButton_StoryMode(true);
        ControllerManager.instance.SetSwitch(true);

        m_resultTalkIdx = await PopupManager.instance.OpenTalkSelectAsync(
            "이정도면 조조공에게 빚은 갚은거겠지.\n이제 돌아가자.",
            "이 자가 원소진영에서 으뜸이라고?\n흥! 이 자의 목을 걸어두어라.");
    }
}
