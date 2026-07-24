using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class StoryMode_Node_31 : StoryModeBaseComponent
{
    protected override async UniTask StartAsync()
    {
        //await FirstPhaseAsync();

        while (m_queTalk.Peek().kr.IsActive() == true)
            m_queTalk.Dequeue();

        GetHero(CharacterName.CaoRen).gameObject.SetActive(false);
        GetHero(CharacterName.CaoCao).gameObject.SetActive(false);

        var guanYu = GetHero(CharacterName.GuanYu);
        var zhangFei = GetHero(CharacterName.ZhangFei);
        zhangFei.SetColorParts(Color.white, _isSetPrev: false);
        zhangFei.element.parts.Find("Head/Eyes").gameObject.SetActive(false);

        guanYu.position = Vector3.zero;
        guanYu.move.SetFlip(true);
        zhangFei.position = guanYu.position + Vector3.right * 5;

        await PopupManager.instance.ShowDimmAsync(false);

        await BattleAsync(zhangFei, guanYu);

        await UniTask.WaitUntil(() => Input.GetKey(KeyCode.Escape));
    }

    async UniTask FirstPhaseAsync()
    {
        var zhangFei = GetHero(CharacterName.ZhangFei);
        zhangFei.SetTalkboxName("??");
        var caoCao = GetHero(CharacterName.CaoCao);
        caoCao.gameObject.SetActive(false);
        var caoRen = GetHero(CharacterName.CaoRen);
        var guanYu = GetHero(CharacterName.GuanYu);
        var zhangLiao = GetHero(CharacterName.ZhangLiao);

        CameraManager.instance.SetCameraPosTarget(caoRen.cameraPos);

        await PopupManager.instance.ShowDimmAsync(false);

        //조인	이 괴물같은 넘..
        caoRen.move.MoveToPointAdd(Vector2.left * .5f);
        zhangFei.move.MoveToPointAdd(Vector3.left);
        await TalkStartAsync();

        caoRen.attack.Rush(zhangFei.position + Vector3.right * 1f);

        await UniTask.WaitUntil(() => (zhangFei.position - caoRen.position).sqrMagnitude < 16);
        zhangFei.position += Vector3.left * 2;
        zhangFei.move.SetFlip(true);
        zhangFei.gameObject.SetActive(false);

        //조인	아??
        TalkAutoClose(0);
        await WaitPointerDown();

        zhangFei.gameObject.SetActive(true);
        CameraManager.instance.SetCameraPosTarget(zhangFei.cameraPos, false);
        await WaitForSeconds(.5f);

        zhangFei.move.MoveToPointAdd(Vector2.right, _isAniPlay: false);
        zhangFei.anim.PlayAttack(true, true);
        await WaitForSeconds(5 / 60f);

        CameraManager.instance.SetCameraPosTarget(null);
        caoRen.anim.Play(CharacterAnimType.Die_2);
        caoRen.talkbox.SetActive(false);
        {
            var target = caoRen.position + Vector3.right * 20;
            target.y = 10;
            await caoRen.transform.DOMove(target, 0.2f);
        }

        await WaitForSeconds(1f);
        Destroy(caoRen.gameObject);

        //장비	"어휴.. 죽이지 않고 패는게        더 힘들구만.."
        await TalkStartAsync();

        zhangFei.move.SetFlip(false);
        caoCao.gameObject.SetActive(true);
        //조조	멈추어라!
        TalkAutoClose(0, true);
        await WaitPointerDown();
        caoCao.talkbox.SetActive(false);

        CameraManager.instance.SetCameraPosTarget(caoCao.cameraPos, false);
        await WaitPointerDown();

        //장비	"흥! 역적 놈이         명을 재촉하는군ㅋ"
        zhangFei.move.MoveToPoint(caoCao.position + Vector3.right * 5f);
        await TalkAutoCloseAsync(0);

        await UniTask.WaitUntil(() => zhangFei.talkbox.isTyping == false);
        caoCao.move.MoveToPointAdd(Vector2.left * .5f);

        await UniTask.WaitUntil(() => zhangFei.move.isMoving == false);
        await WaitPointerDown();
        zhangFei.talkbox.SetActive(false);

        //조조	역적이라고??
        //장비	" '동소'라고 했던가? 네 놈을 위왕에        봉하라는 상소를 올렸다지? ㅋ"
        await TalkStartAsync(2);

        //조조	"어리석은!! 난 한 나라의 신하일 뿐,        천하를 편안하게 하는 것 외엔 관심없다!"
        caoCao.anim.PlayAttack();
        //장비	ㅋㅋ 곧 죽을 놈이 말이 많구나!!
        await TalkStartAsync(2);

        //조조	이야앗!!
        await TalkAutoCloseAsync(0);
        zhangFei.anim.AttackMotionFirstFrame();
        await UniTask.WaitUntil(() => caoCao.talkbox.isTyping == false);

        zhangFei.anim.AttackMotionEnd();
        await caoCao.attack.RushAsync(zhangFei.position + Vector3.left * 3f);
        zhangFei.transform.DOMoveX(zhangFei.position.x + 5, 0.2f).Forget();

        guanYu.move.SetFlip(true);
        guanYu.position = zhangFei.position + Vector3.left * .5f;
        guanYu.anim.PlayAttack(true, true);

        await WaitForSeconds(1f);

        //장비	!?
        TalkAutoClose(0);

        //조조	..? 관공?
        await TalkStartAsync();
        zhangFei.talkbox.SetActive(false);

        //관우 이번에는 번개가 안치는군요, 승상.
        //조조 ??
        await TalkStartAsync(2);

        {
            var target = caoCao.position + Vector3.right;
            target.y -= .3f;
            await zhangLiao.move.DashAsync(target, 0, 0.2f);
        }

        //장료	주공, 일단 자리를 피하시지요.
        //조조	..번개?
        await TalkStartAsync(2);

        CameraManager.instance.SetCameraPosTarget(null);

        zhangLiao.move.Dash(zhangLiao.position + Vector3.left * 10, 0);
        caoCao.move.Dash(caoCao.position + Vector3.left * 10, 0);

        await WaitForSeconds(1f);
        Destroy(caoCao.gameObject);
        Destroy(zhangLiao.gameObject);

        //장비	"ㅋㅋ 악귀놈, 이제야 온 것이냐ㅋ        어차피 네놈 또한.."
        //관우  "3년동안 제삿밥만 먹더니        살이 좀 빠졌구나, 막내야."
        //장비 고생하긴 했지..음 ??
        await TalkStartAsync(3);

        //장비  뭐야!! 어떻게 안것이냐, 네놈!!
        zhangFei.anim.PlayAttack(true, true);
        zhangFei.SetColorParts(Color.white, _isSetPrev: false);
        zhangFei.element.parts.Find("Head/Eyes").gameObject.SetActive(false);
        //관우  ".. 배불뚝이.. 짐승같은 눈매..      아름답지 못한 턱수염..그리고.."
        await TalkStartAsync(2);

        //장비  "시끄럽다!! 큰 형님을 죽음으로 몰고,       역적 조조에게 붙어버린 배신자놈!!"
        zhangFei.anim.PlayAttack(true, true);
        await TalkStartAsync();

        //관우 ㅋㅋㅋ 오거라ㅋ
        await TalkStartAsync();

        await BattleAsync(zhangFei, guanYu);

        PopupManager.instance.AlertShow("스토리를_완료했습니다.");
        await WaitPointerDown();
    }

    async UniTask BattleAsync(CharacterComponent zhangFei, CharacterComponent guanYu)
    {
        var weapon = guanYu.element.parts.Find("Weapon").GetChild(0);

        UnityAction actionDefence = () =>
        {
            weapon.localPosition = new Vector3(-1.18f, 1.3f);
            weapon.rotation = Quaternion.Euler(0, 0, -165.2f);
        };

        UnityAction actionIdle = () =>
        {
            weapon.localPosition = new Vector3(.333f, .305f);
            weapon.rotation = Quaternion.Euler(0, 0, -108f);
        };

        CameraManager.instance.SetCameraPosTarget(zhangFei.cameraPos, false);

        // 공격!
        zhangFei.anim.PlayAttack(true, true);
        zhangFei.transform.DOMoveX(guanYu.position.x + 3, .2f).Forget();
        await WaitForSeconds(.1f);

        actionDefence();
        guanYu.move.MoveToPointAdd(Vector2.left * .5f);

        await WaitForSeconds(.4f);

        // 공격!
        zhangFei.anim.PlayAttack(true, true);
        await WaitForSeconds(.1f);
        guanYu.move.MoveToPointAdd(Vector2.left * .5f);

        await WaitForSeconds(.4f);

        // 강 공격!
        zhangFei.anim.AttackMotionFirstFrame();
        await WaitForSeconds(.5f);
        zhangFei.anim.AttackMotionEnd();
        zhangFei.anim.PlayAttack(true, true);

        // 관우 피하기!!
        await WaitForSeconds(.1f);
        await guanYu.move.DashAsync(guanYu.position + Vector3.left);

        // 장비 돌격!!
        zhangFei.attack.Rush(guanYu.position);

        // 가까이 올때까지 대기
        await UniTask.WaitUntil(() => (zhangFei.position - guanYu.position).sqrMagnitude < 16);

        guanYu.move.SetFlip(false);
        await guanYu.move.DashAsync(guanYu.position + Vector3.right * 6f, 0, 0.2f);
        guanYu.move.SetFlip(false);
        actionIdle();

        await WaitForSeconds(1f);
        zhangFei.move.SetFlip(true);

        //장비	??
        zhangFei.move.MoveToPointAdd(Vector2.right * .5f);
        await TalkAutoCloseAsync();

        zhangFei.attack.Rush(guanYu.position);
        await UniTask.WaitUntil(() => (zhangFei.position - guanYu.position).sqrMagnitude < 9);
        actionDefence();
        await guanYu.transform.DOMoveX(guanYu.position.x + 4, .2f);

        await WaitForSeconds(.3f);

        zhangFei.anim.PlayAttack(true, true);
        await WaitForSeconds(.2f);
        guanYu.move.MoveToPointAdd(Vector2.right * .5f);
        await WaitForSeconds(.3f);

        zhangFei.attack.Rush(guanYu.position + Vector3.right * 5);

        float time = Time.time + 0.2f;
        while (time > Time.time)
        {
            var pos = guanYu.position;

            pos.x = Mathf.Max(guanYu.position.x, zhangFei.position.x + 2);
            guanYu.position = pos;
            await UniTask.NextFrame();
        }
        guanYu.move.MoveToPointAdd(Vector2.right);

        await WaitForSeconds(.5f);

        zhangFei.anim.PlayAttack(true, true);

        await WaitForSeconds(.1f);

        actionIdle();
        await guanYu.move.DashAsync(guanYu.position + Vector3.left, 7);
        guanYu.move.SetFlip(true);
        guanYu.move.MoveToPointAdd(Vector2.left * .5f);

        await WaitForSeconds(.5f);
        zhangFei.move.SetFlip(false);

        //장비	"뭐냐!? 어찌하여 공격을        하지 않는 것이냐! ? "
        //관우	"(3년간 술과 고기를 멀리 하더니,        무의의 경지에 올랐구나.)"
        await TalkStartAsync(2);

        //관우	"ㅋㅋㅋ 겨우 이정도가        장익덕의 힘이더냐?ㅋ"
        guanYu.anim.PlayAttack(true);
        await TalkStartAsync();

        //장비	이놈이!!
        zhangFei.move.MoveToPointAdd(Vector2.right * .5f);
        await TalkStartAsync();

        zhangFei.attack.Rush(guanYu.position + Vector3.right * 2);
        await UniTask.WaitUntil(() => (zhangFei.position - guanYu.position).sqrMagnitude < 9);

        //관우	크윽..
        TalkAutoClose(0);
        guanYu.attack.SetActive_Weapon(false);
        guanYu.anim.Play(CharacterAnimType.Knockdown);
        await guanYu.transform.DOMoveX(guanYu.position.x - 3, 0.2f);

        zhangFei.anim.AttackMotionFirstFrame();
        await WaitPointerDown();
        guanYu.talkbox.SetActive(false);

        zhangFei.anim.AttackMotionEnd();
        zhangFei.anim.Play(CharacterAnimType.Idle);

        //장비	"뭐냐!! 네 녀석이라면 충분히        막을 수 있었을텐데..이놈!!"
        zhangFei.move.MoveToPointAdd(Vector3.left * .5f);
        await TalkStartAsync();

        //관우	이제야 안심이구나ㅋ
        await TalkStartAsync();

        //장비 뭐..뭐라 ??
        await TalkStartAsync();

        //관우	"난 형님을 죽이고 천하를        공포에 떨어트린 악귀.."
        //관우  "너는.. 그 악귀를 물리친       영웅이 되는 것이다..크읔"
        //장비  "이 젠장 먹을 형님이 대체       무슨 소리를 하는거요!!"
        //관우  "형님이 이루고자 했던 세상에서     너는 백성들이 기댈 수 있는 무결의     신이 되어야 하느니라."
        await TalkStartAsync(4);

        zhangFei.move.MoveToPointAdd(Vector3.left * .5f);

        //장비 흑흑..형님ㅜㅜ
        await TalkStartAsync();

        //관우  "백성들에게는 그런 신의 존재가 필요하다.       그러니 네가.."
        await TalkAutoCloseAsync(0);
        await UniTask.WaitUntil(() => guanYu.talkbox.isTyping == false);
        await WaitPointerDown();

        var fanJiang = GetHero("FanJiang");
        {
            var target = zhangFei.position + Vector3.right * 2;
            target.y -= .3f;
            await fanJiang.transform.DOMove(target, 0.2f);
        }
        guanYu.talkbox.SetActive(false);

        //장비	..?
        //범강	하하.. 잡았다..
        await TalkStartAsync(2);

        var zhangDa = GetHero("ZhangDa");
        {
            var target = zhangFei.position + Vector3.right * 3;
            target.y += .3f;
            await zhangDa.transform.DOMove(target, 0.2f);
        }

        zhangFei.anim.Play(CharacterAnimType.Knockdown);
        //장달	"내가 이 괴물같은 녀석의        심장을 뚫었다!!"
        //장비 크읔..
        //관우	이..이 버러지같은 것들이!!
        await TalkStartAsync(3);

        guanYu.attack.SetActive_Weapon(true);
        guanYu.attack.Rush(zhangFei.position + Vector3.right * 5f);

        await WaitForSeconds(.1f);

        zhangFei.anim.Play(CharacterAnimType.Die_1);
        zhangFei.move.SetFlip(true);
        zhangFei.move.MoveToPointAdd(Vector2.right, _isAniPlay: false);

        {
            var target = fanJiang.position + Vector3.right * 20;
            target.y += 10;
            fanJiang.transform.DOMove(target, 0.2f).Forget();
        }
        {
            var target = zhangDa.position + Vector3.right * 20;
            target.y -= 5;
            zhangDa.transform.DOMove(target, 0.2f).Forget();
        }

        await WaitForSeconds(1f);
        guanYu.move.MoveToPointAdd(Vector2.left * .5f, false);

        //관우	마..막내야??
        await TalkStartAsync();

        guanYu.attack.SetActive_Weapon(false);
        guanYu.anim.Play(CharacterAnimType.Knockdown);

        //관우	크윽.. 크아아아아ㅏㅏ
        await TalkStartAsync();
    }
}
