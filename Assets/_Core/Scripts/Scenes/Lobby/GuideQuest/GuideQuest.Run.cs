using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class GuideQuestComponent
{
    async UniTask MoveAsync()
    {
        var main = TeamManager.instance.mainHero;

        bool isComplete = false;
        while (true)
        {
            await UniTask.WaitUntil(() => ControllerManager.instance.isDoing == true);

            Vector3 prevPosition = main.position;

            while (ControllerManager.instance.isDoing == true)
            {
                if ((prevPosition - main.position).sqrMagnitude > 4)
                {
                    isComplete = true;
                    break;
                }
                await UniTask.NextFrame(destroyCancellationToken);
            }

            if (isComplete)
            {
                TutorialManager.instance.Update();
                main.talkbox.SetActive(false);
                break;
            }
        }
    }

    async UniTask NormalAttackAsync()
    {
        var main = TeamManager.instance.mainHero;

        while (true)
        {
            await UniTask.WaitUntil(() => main.attack.isAttackPush == true);

            if (TutorialManager.instance.Update() == true)
                return;

            await UniTask.WaitUntil(() => main.attack.isAttackPush == false);
        }
    }

    async UniTask MainSkillUseAsync()
    {
        var main = TeamManager.instance.mainHero;

        while (true)
        {
            await UniTask.WaitUntil(() => main.attack.isUseSkill == true);

            if (TutorialManager.instance.Update() == true)
                return;

            await UniTask.WaitUntil(() => main.attack.isUseSkill == false);
        }
    }

    async UniTask DashUseAsync()
    {
        var main = TeamManager.instance.mainHero;

        while (true)
        {
            await UniTask.WaitUntil(() => main.move.isDash == true);

            if (TutorialManager.instance.Update() == true)
                return;

            await UniTask.WaitUntil(() => main.move.isDash == false);
        }
    }

    async UniTask StoryModePlayAsync()
    {
        while (true)
        {
            if (BannerComponent.instance.story.isActive == false)
            {
                TeamManager.instance.mainHero.talkbox.Start(destroyCancellationToken, "스토리_모드_해금!");
                Signal.instance.UnlockStoryMode.Emit();
            }

            await UniTask.WaitUntil(() => DataManager.storyMode.isRunning == true, cancellationToken: destroyCancellationToken);

            if (TutorialManager.instance.Update() == true)
                return;
        }
    }

    async UniTask CharacterDeployAsync()
    {
        while (true)
        {
            await UniTask.WaitUntil(() => TeamManager.instance.members.Count > 1, cancellationToken: destroyCancellationToken);

            if (TutorialManager.instance.Update() == true)
                return;
        }
    }
}
