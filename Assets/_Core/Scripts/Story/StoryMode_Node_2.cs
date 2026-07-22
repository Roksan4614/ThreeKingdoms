using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Linq;
using UnityEngine;

public class StoryMode_Node_2 : StoryModeBaseComponent
{
    protected async override UniTask StartAsync()
    {
        await Phase_First();

        // TEST fist 넘기려고
        //while (m_queTalk.Peek().target != CharacterName.CaoCao.ToString())
        //    m_queTalk.Dequeue();

        await SetNextPhaseAsync();

        await Phase_Second();
    }

    async UniTask Phase_First()
    {
        PopupManager.instance.ShowDimm(false);

        var token = m_cts.Token;

        CameraManager.instance.SetCameraPosTarget(mainHero.cameraPos);

        // 좌측에서 우측으로 조금 이동해주자
        var heroes = phase.heroes.Values.ToList();
        var enemies = phase.enemies.Values.ToList();

        float time = Time.time;
        while (time + .5f > Time.time)
        {
            foreach (var hero in heroes)
                hero.move?.OnMoveUpdate(Vector2.right * 10);

            await UniTask.NextFrame(cancellationToken: m_cts.Token);
        }

        foreach (var hero in heroes)
            hero.move?.MoveStop();

        var caoRen = mainHero;

        // 흥! 아직도 도적 떼가 설치다니!!
        await TalkStartAsync();

        // 죽여주마!
        caoRen.anim.PlayAttack();
        TalkAutoClose();

        await UniTask.WaitForSeconds(1f);

        foreach (var h in heroes)
            h.SetActive_HP(true);
        foreach (var e in enemies)
            e.SetActive_HP(true);

        ControllerManager.instance.SetSwitch(true);
        ControllerManager.instance.DashTimerStartAsync().Forget();
        ControllerManager.instance.SetActiveButton_StoryMode(true);

        TeamManager.instance.AddBuff(BuffType.BUFF_NO_DIE);
        TeamManager.instance.SetState(CharacterStateType.Battle);

        await UniTask.WaitUntil(() => StageManager.instance.IsAllDead(), cancellationToken: token);

        ControllerManager.instance.SetSwitch(false);
        await UniTask.WaitForSeconds(1f, cancellationToken: token);

        foreach (var h in heroes)
            h.SetActive_HP(false);

        ControllerManager.instance.SetActiveButton_StoryMode(false);
        // 흥 별것도 아닌것들이
        TalkAutoClose(0);

        {
            //부하가 나타나는걸 표현하자
            var etc = phase.GetHero(CharacterName.Etc);
            Vector3 pos, target;
            target = pos = mainHero.transform.position;
            pos.x += pos.x > 0 ? -12 : 12;
            pos.y += 0.2f;
            etc.transform.position = pos;
            etc.gameObject.SetActive(true);

            //조인은 앞으로 좀 이동하자.
            caoRen.move.MoveToPoint(caoRen.position + Vector3.right * (caoRen.move.isFlip ? 2 : -2));

            var lookAt = target - etc.transform.position;
            while (lookAt.sqrMagnitude >= 25)
            {
                etc.move.OnMoveUpdate(lookAt.normalized * 20);
                lookAt = target - etc.transform.position;

                await UniTask.NextFrame(cancellationToken: token);
            }
            etc.move.MoveStop();
            etc.UpdateSortingOreder(true);

            await UniTask.WaitUntil(() => caoRen.talkbox.isTyping == false);
            await WaitPointerDown();

            caoRen.talkbox.SetActive(false);
            caoRen.move.LookTarget(etc);
            // 대장!! 조조님이 드디어 거병하셨답니다!
            await TalkStartAsync();
        }

        // 맹덕 형님이??
        await TalkStartAsync();
        // 좋다!! 우리도 가서 힘을 보태자. 너희들도 나를 따라오너라!
        mainHero.anim.PlayAttack();
        await TalkStartAsync();

        CameraManager.instance.SetCameraPosTarget(null);

        PopupManager.instance.ShowDimm(true, _duration: 1f);

        //밖으로 나가주자
        {
            time = Time.time;
            var lookAt = mainHero.transform.position.x > 0 ? Vector2.left : Vector2.right;
            while (time + 1f > Time.time)
            {
                foreach (var hero in heroes)
                    hero.move.OnMoveUpdate(lookAt * 30);

                await UniTask.NextFrame(cancellationToken: m_cts.Token);
            }
        }
    }

    async UniTask Phase_Second()
    {
        var heroes = phase.heroes.Values.ToList();
        foreach (var h in heroes)
            h.SetActive_HP(false);

        CameraManager.instance.SetCameraPosTarget(mainHero.cameraPos);
        await PopupManager.instance.ShowDimmAsync(false, _duration: 1f);
        var token = m_cts.Token;

        var caoCao = mainHero;

        // 이 난세를 헤쳐 나가려면 힘을 키워야 한다.
        await TalkStartAsync();
        // 걱정마라 아만!내가 있는데 뭐가 걱정인게야 캬캬캬!
        var cDun = phase.GetHero(CharacterName.XiahouDun);
        cDun.anim.PlayAttack();
        await TalkStartAsync();
        // 형님 때문에 더 걱정입니다.저번에도 혼자 돌격하시고.
        await TalkStartAsync();
        // 뒤에서 활이나 쏘는 주제에 말이 많구나ㅋㅋ
        await TalkStartAsync();
        // 야야 가만히 있지만 말고 니들도 나가서…
        caoCao.move.LookTarget(cDun);
        await TalkStartAsync();

        // 주공! 성 밖에 1000여명 되는 무리들이 이 쪽을 향해 오고 있습니다!
        var etc = phase.GetHero(CharacterName.Etc);
        etc.move.LookTarget(mainHero);
        caoCao.move.SetFlip(true);
        await TalkStartAsync();
        etc.move.SetFlip(true);

        // 음??
        TalkAutoClose();
        // 호오
        await TalkStartAsync();

        CameraManager.instance.SetCameraPosTarget(null);

        var caoRen = phase.GetHero(CharacterName.CaoRen);

        // 조인은 이동하고 카메라는 천천히 조인쪽으로 이동하자
        {
            var camera = CameraManager.instance.main;

            var target = caoRen.cameraPos.position;
            target.z = camera.transform.position.z;

            await camera.transform.DOMove(target, 2f).ToUniTask();

            CameraManager.instance.SetCameraPosTarget(caoRen.cameraPos, false);

            var caoRenTeam = phase.heroes.Skip(6).ToList();
            var caoCaoTeam = phase.heroes.Take(3).ToList();
            foreach (var h in caoRenTeam)
                h.Value.anim.Play(CharacterAnimType.Walk);
            foreach (var h in caoCaoTeam)
                h.Value.anim.Play(CharacterAnimType.Walk);

            foreach (var h in caoCaoTeam)
            {
                var pos = h.Value.position;
                pos.x += 10;
                h.Value.position = pos;
            }

            var distance = caoRen.position - caoCao.position;
            while (distance.sqrMagnitude >= 25)
            {
                await UniTask.NextFrame();

                foreach (var h in caoRenTeam)
                    h.Value.rig.linearVelocityX = -5;

                foreach (var h in caoCaoTeam)
                    h.Value.rig.linearVelocityX = 10;

                distance = caoRen.position - mainHero.position;
            }

            foreach (var h in caoRenTeam)
                h.Value.move.MoveStop();
            foreach (var h in caoCaoTeam)
                h.Value.move.MoveStop();
        }

        // 하하하!!맹덕 형님! 나 자효가 왔습니다!!
        await TalkStartAsync();
        // 뭐야ㅋㅋ 자효잖아?ㅋ
        TalkAutoClose(0);

        // 오오, 자효!! 이렇게 와주니 정말 고맙구만!!
        await TalkStartAsync();
        cDun.talkbox.SetActive(false);

        // 형님, 사적인 정은 이제 접어두겠소.
        await TalkStartAsync();

        // 오늘부로 저와 제 군사는 오직 주공의 명만을 따를 것이니, 부디 천하를 바로잡아 주십시오!
        caoRen.anim.PlayAttack();
        await TalkStartAsync();

        PopupManager.instance.AlertShow("조인이_진영에_합류했습니다.");
        await UniTask.WaitForSeconds(.5f);

        // 일어나시게ㅋ 이제 곧 큰 싸움이 일어날텐데, 자효 그대의 활약을 기대하겠네.
        await TalkStartAsync();

        PopupManager.instance.AlertShow("스토리를_완료했습니다.");
        // 주공!!
        await TalkStartAsync();
    }
}
