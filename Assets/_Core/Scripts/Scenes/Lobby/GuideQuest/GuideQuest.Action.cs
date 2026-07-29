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
            await UniTask.WaitUntil(() => main.attack.isAttack == true);

            if (TutorialManager.instance.Update() == true)
                return;

            await UniTask.WaitUntil(() => main.attack.isAttack == false);
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
