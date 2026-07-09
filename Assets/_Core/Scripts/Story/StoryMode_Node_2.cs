using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;

public class StoryMode_Node_2 : StoryModeBaseComponent
{
    protected override void Start()
    {
        base.Start();
    }

    protected async override UniTask StartAsync()
    {
        CameraManager.instance.SetCameraPosTarget(mainHero.element.cameraPos);

        var token = m_cts.Token;

        // 좌측에서 우측으로 조금 이동해주자
        var heroes = phase.heroes.Values.ToList();

        float time = Time.time;

        while (time + .5f > Time.time)
        {
            foreach (var hero in heroes)
                hero.move.OnMoveUpdate(Vector2.right * 10);

            await UniTask.NextFrame(cancellationToken: m_cts.Token);
        }

        foreach (var hero in heroes)
            hero.move.MoveStop();

        // 흥! 아직도 도적 떼가 설치다니!!
        await TalkStartAsync();

        ControllerManager.instance.SetSwitch(true);
        ControllerManager.instance.DashTimerStartAsync().Forget();
        ControllerManager.instance.SetActiveButton_StoryMode();

        // 죽여주마!
        TalkAutoClose();
    }
}
