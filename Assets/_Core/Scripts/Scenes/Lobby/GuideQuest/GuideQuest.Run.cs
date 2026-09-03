using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public partial class GuideQuestComponent
{
    List<KeyCode> m_keycode = new();

    void AddKeyCode(KeyCode _keyCode)
    {
        if (Input.GetKeyDown(_keyCode) && m_keycode.Contains(_keyCode) == false)
            m_keycode.Add(_keyCode);
    }

    async UniTask MoveAsync()
    {
        bool isComplete = false;
        while (true)
        {
            var main = TeamManager.instance.mainHero;

            await UniTask.WaitUntil(() => ControllerManager.instance.isDoing == true);

            //if (ControllerManager.instance.isKeyboardMode)
            //{
            //    while (m_keycode.Count < 4)
            //    {
            //        AddKeyCode(KeyCode.W);
            //        AddKeyCode(KeyCode.A);
            //        AddKeyCode(KeyCode.S);
            //        AddKeyCode(KeyCode.D);

            //        await UniTask.NextFrame(destroyCancellationToken);
            //    }
            //    isComplete = true;
            //}
            //else
            {

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
        while (true)
        {
            var main = TeamManager.instance.mainHero;

            await UniTask.WaitUntil(() => main.attack.isAttackPush == true);

            if (TutorialManager.instance.Update() == true)
                return;

            await UniTask.WaitUntil(() => main.attack.isAttackPush == false);
        }
    }

    async UniTask MainSkillUseAsync()
    {
        while (true)
        {
            var main = TeamManager.instance.mainHero;

            await UniTask.WaitUntil(() => main.attack.isUseSkill == true);

            if (TutorialManager.instance.Update() == true)
                return;

            await UniTask.WaitUntil(() => main.attack.isUseSkill == false);
        }
    }

    async UniTask DashUseAsync()
    {
        while (true)
        {
            var main = TeamManager.instance.mainHero;

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
