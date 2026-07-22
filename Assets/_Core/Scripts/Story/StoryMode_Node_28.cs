using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine;

public class StoryMode_Node_28 : StoryModeBaseComponent
{
    protected override async UniTask StartAsync()
    {
        //await FirstPhaseAsync();

        while (m_queTalk.Peek().kr.IsActive() == true)
            m_queTalk.Dequeue();

        await SetNextPhaseAsync();

        await SecondPhaseAsync();
    }

    async UniTask FirstPhaseAsync()
    {
        var guanYu = GetHero(CharacterName.GuanYu);
        var zhangLiao = GetHero(CharacterName.ZhangLiao);
        var xiahouDun = GetHero(CharacterName.XiahouDun);
        var xiahouYuan = GetHero(CharacterName.XiahouYuan);

        CameraManager.instance.SetCameraPosTarget(zhangLiao.cameraPos);

        await PopupManager.instance.ShowDimmAsync(false);

        //장료	과연 대단하십니다, 관장군.
        //장료	"하북 지역을 1년만에 평정하시고,        쉬지도 않은 채 곧바로 형주 정벌이라니.."
        //관우	"죄책감이 날 짓눌러 견딜 수가 없소.        멈출 수가 없구려."
        await TalkStartAsync(3);

        //하후돈	"하하하. 역시 관장군이오.        대단하십니다!"
        xiahouDun.anim.PlayAttack();
        await TalkStartAsync();

        //관우	..
        //하후돈	하하;;
        await TalkStartAsync(2);

        //하후돈	무안하게시리.. - _-
        //하후연 "아무래도 미움을     단단히 산 모양이군요."
        xiahouDun.move.SetFlip(true);
        await TalkStartAsync(2);

        //관우	"자아. 빠르게 진격하겠소.        전군, 출진하라!"
        guanYu.anim.PlayAttack();
        await TalkStartAsync();

        //하후돈	..
        TalkAutoClose(0);
        xiahouDun.move.SetFlip(false);

        zhangLiao.anim.PlayAttack();

        await WaitForSeconds(1f);
        await PopupManager.instance.ShowDimmAsync(true, _duration: 1);
    }

    async UniTask SecondPhaseAsync()
    {
        var enemy = GetHero("Enemy");
        var etc = GetHero("Etc");
        var guanYu = GetHero(CharacterName.GuanYu);
        var zhangLiao = GetHero(CharacterName.ZhangLiao);
        var xiahouDun = GetHero(CharacterName.XiahouDun);
        var zhangFei = GetHero("??");

        CameraManager.instance.SetCameraPosTarget(enemy.cameraPos);

        await PopupManager.instance.ShowDimmAsync(false);

        //Enemy	으윽.. 이 악귀놈..
        enemy.move.MoveToPointAdd(Vector2.left * .5f);
        await TalkStartAsync();

        guanYu.move.MoveToPointAdd(Vector2.left);

        //Enemy	으아악!!
        TalkAutoClose(0);
        enemy.MoveSpeedMultiple(2f);
        enemy.move.MoveToPoint(guanYu.position);
        await UniTask.WaitUntil(() => (enemy.position - guanYu.position).sqrMagnitude < 16);
        enemy.move.MoveStop();
        enemy.anim.Play(CharacterAnimType.Die_1);

        //관우 스킬
        {
            guanYu.move.MoveStop();

            DateTime dt = DateTime.Now.AddSeconds(0.1f);
            EffectWorker.instance.Dash(guanYu, true);

            // 카메라 흔들기
            CameraManager.instance.Shake();

            enemy.talkbox.SetActive(false);
            enemy.anim.Play(CharacterAnimType.Die_1);

            guanYu.anim.AttackMotionFirstFrame(CharacterAnimType.Attack_Move, 1);
            await DOTween.To(() => guanYu.position, _pos => guanYu.rig.MovePosition(_pos), enemy.position + Vector3.left * 4f, 0.2f).SetUpdate(UpdateType.Fixed)
                .OnUpdate(() =>
                {
                    if (DateTime.Now > dt)
                    {
                        EffectWorker.instance.Dash(guanYu, true);
                        dt = DateTime.Now.AddSeconds(10);

                        guanYu.anim.AttackMotionEnd();
                        guanYu.attack.ShowSlashEffect(true);
                    }
                }).ToUniTask(cancellationToken: m_cts.Token);
        }

        await WaitPointerDown();

        //관우	악귀..라..
        guanYu.move.SetFlip(true);
        TalkAutoClose(0);

        {
            var target = guanYu.position + Vector3.right * 4;
            target.y = zhangLiao.position.y;

            await zhangLiao.move.DashAsync(target, 0);
        }

        await UniTask.WaitUntil(() => guanYu.talkbox.isTyping == false);
        await WaitPointerDown();
        guanYu.talkbox.SetActive(false);

        //장료	"끝이군요. 이로써        형주까지 우리 수중에.."
        TalkAutoClose(0);
        await WaitForSeconds(1f);

        //관우	..?
        isLock_MoveCamera = true;
        guanYu.move.SetFlip(false);
        await TalkStartAsync();

        guanYu.move.MoveToPoint(etc.position + Vector3.right * 5f);

        //장료 장군?
        await TalkStartAsync();

        CameraManager.instance.SetCameraPosTarget(guanYu.cameraPos, false);
        await UniTask.WaitUntil(() => guanYu.move.isMoving == false);

        //Etc	사..살려주세요ㅜㅜ
        isLock_MoveCamera = true;
        await TalkStartAsync();

        {
            var target = guanYu.position + Vector3.right * 4f;
            target.y = xiahouDun.position.y;

            await xiahouDun.move.DashAsync(target, 0);
            xiahouDun.move.MoveToPointAdd(Vector2.left);

            target.y = zhangLiao.position.y;
            await zhangLiao.move.DashAsync(target, 0);
        }

        //하후돈	"뭐하는거요, 장군.            그는 일반 백성일 뿐이오."
        //관우	"후훗.. 서주가 떠올라 흥분되오?            나 역시 그렇군ㅋㅋ"
        await TalkStartAsync(2);

        guanYu.anim.AttackMotionFirstFrame();

        //하후돈	그만두라고 이 자식아!!
        TalkAutoClose(0);

        //장료	!!
        await TalkStartAsync();

        await zhangFei.move.DashAsync(etc.position + Vector3.left * 4);





        PopupManager.instance.AlertShow("스토리를_완료했습니다.");
        await WaitPointerDown();

    }
}
