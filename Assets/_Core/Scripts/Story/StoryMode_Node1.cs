using Cysharp.Threading.Tasks;
using UnityEngine;

public class StoryMode_Node1 : StoryModeBaseComponent
{
    protected override void Start()
    {
        base.Start();

        var character = transform.GetComponent<CharacterComponent>("Hero/GuanYu");
        character.SetHeroData_StoryModeMain(character.name);

        TeamManager.instance.InitializeAsync_StoryMode(character).Forget();
        ControllerManager.instance.DashTimerStartAsync().Forget();

        CameraManager.instance.SetCameraPosTarget(character.element.cameraPos);
        Signal.instance.ConnectMainHero.Emit(character);
    }
}
