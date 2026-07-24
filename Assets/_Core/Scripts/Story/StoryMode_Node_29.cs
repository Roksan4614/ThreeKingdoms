using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine;

public class StoryMode_Node_29 : StoryModeBaseComponent
{
    protected override async UniTask StartAsync()
    {
        await FirstPhaseAsync();

        //while (m_queTalk.Peek().kr.IsActive() == true)
        //    m_queTalk.Dequeue();

        await SetNextPhaseAsync();

        await SecondPhaseAsync();
    }

    async UniTask FirstPhaseAsync()
    {
        var guanYu = GetHero(CharacterName.GuanYu);
        var zhangLiao = GetHero(CharacterName.ZhangLiao);
        var xiahouDun = GetHero(CharacterName.XiahouDun);
        var xiahouYuan = GetHero(CharacterName.XiahouYuan);
        var zhaoYun = GetHero(CharacterName.ZhaoYun);

        CameraManager.instance.SetCameraPosTarget(guanYu.cameraPos);

        await PopupManager.instance.ShowDimmAsync(false);

        //하후돈	"그 견고하던 강동을 평정하는데        1년이 채 안걸리다니!!"
        await TalkStartAsync();

        xiahouDun.anim.PlayAttack();
        //하후돈	역시 전장의 귀신! 관 장군이오!!
        //관우	.. - -
        await TalkStartAsync(2);

        xiahouDun.move.MoveToPointAdd(Vector2.left * .5f);
        //하후돈	캬캬! 또 말이 없으시군ㅋㅋ
        //관우	"먼저 귀환들 하시오.         난 따로 볼일 좀 보고 가겠소."
        //하후돈	"ㅋㅋ 늦으시면 승상께서 내리신 술은        내가 다 마셔버릴 거요!캬캬캬"
        //하후연 체통좀 지키시오, 형님.
        await TalkStartAsync(4);

        CameraManager.instance.SetCameraPosTarget(null);

        xiahouDun.move.MoveToPointAdd(Vector2.left * 20);
        xiahouYuan.move.MoveToPointAdd(Vector2.left * 20);
        zhangLiao.move.MoveToPointAdd(Vector2.left * 5);

        await WaitForSeconds(1f);
        guanYu.move.MoveToPointAdd(Vector3.right * 15f, false);
        await UniTask.WaitUntil(() => zhangLiao.move.isMoving == false);

        await WaitForSeconds(1f);
        zhangLiao.move.SetFlip(true);

        //장료	흐음..
        await TalkStartAsync(1, true);

        await PopupManager.instance.ShowDimmAsync(true);

        Destroy(xiahouDun.gameObject);
        Destroy(xiahouYuan.gameObject);

        guanYu.position = Vector3.zero;

        zhangLiao.position = guanYu.position + Vector3.left * 20f;
        zhangLiao.gameObject.SetActive(false);

        CameraManager.instance.SetCameraPosTarget(guanYu.cameraPos);

        PopupManager.instance.ShowDimm(false);
        await guanYu.move.MoveToPointAsync(guanYu.position + Vector3.right * 3f);

        //조운 돌격
        guanYu.anim.PlayAttack(true, true);
        await zhaoYun.attack.RushAsync(guanYu.position + Vector3.right, false);

        await guanYu.transform.DOMoveX(guanYu.position.x - 5f, 0.2f);

        await WaitForSeconds(1f);

        //조운	!?
        await TalkStartAsync();

        //관우	창끝이 더 날카로워졌군, 자룡.
        guanYu.move.MoveToPointAdd(Vector2.right);
        await TalkStartAsync(1, true);

        //조운	"그냥 얌전히 목을 내어        주실 순 없으십니까 ? "
        //관우  그럴 수만은 없다네..
        await TalkStartAsync(2, true);

        //조운  "관우님도 느끼고 있으실 것입니다.   아수라의 기운을.."
        //관우	"아무래도 신선술을        익힌 모양이군.."
        zhaoYun.move.MoveToPointAdd(Vector2.left * .5f);
        await TalkStartAsync(2, true);

        //관우	"허나, 자네에게 내 목을        내어줄 순 없네."
        //조운	..? 무슨 말씀이신지..
        guanYu.move.SetFlip(false);
        await TalkStartAsync(2, true);

        //관우	그냥 지나가 주시게, 자룡.
        guanYu.move.MoveToPointAdd(Vector2.left * .5f);
        await TalkStartAsync(1, true);

        //조운	안됩니다. 더 늦었다간..
        zhaoYun.move.MoveToPointAdd(Vector2.left);
        await TalkStartAsync();

        //관우	곧 끝이 날 걸세.
        //조운..
        //조운  "그렇군요. 알겠습니다.     그 때까지 화기를 잘 다루십시오."
        await TalkStartAsync(3, true);

        //관우	"알고 있네. 혹시..        만약에 말일세.."
        guanYu.move.SetFlip(true);
        await TalkAutoCloseAsync(0);
        await UniTask.WaitUntil(() => guanYu.talkbox.isTyping == false);
        await WaitPointerDown();

        zhangLiao.gameObject.SetActive(true);
        await zhangLiao.move.DashAsync(guanYu.position + Vector3.left * 4f, 0, 0.2f);

        //장료	장군!!
        await TalkAutoCloseAsync(0, true);
        await UniTask.WaitUntil(() => guanYu.talkbox.isTyping == false);
        await WaitPointerDown();
        guanYu.talkbox.SetActive(false);
        zhangLiao.talkbox.SetActive(false);

        //조운	".. 전 이만 가보겠습니다.        늦기 전에 찾아뵙지요."
        //관우	음.. 알겠네.
        zhaoYun.move.MoveToPointAdd(Vector3.right, false);
        await TalkStartAsync(2, true);

        zhaoYun.move.Dash(zhaoYun.position + Vector3.right, 10);

        //장료	"(누구였지? 아무런 기척도        못 느꼈었는데..)"
        await TalkStartAsync();

        //관우	무슨일인가, 문원.
        guanYu.move.SetFlip(false);
        await TalkStartAsync();

        zhangLiao.move.MoveToPointAdd(Vector3.right * .5f);

        //장료	"아!! 큰일났습니다. 지금 허도에 괴물같은        자가 나타나 궁으로 돌격하고 있습니다!"
        //관우 괴물??
        //장료  "모두가 공포에 질려 꿈쩍도       못하고 잇습니다."
        //장료  "아무래도 승상을 노리는 것 같은데,      그 괴물을 상대할 자는 장군 밖에 없습니다!"
        await TalkStartAsync(4);

        guanYu.move.MoveToPointAdd(Vector3.left * .5f);

        //관우	.. 앞장 서시게.
        await TalkStartAsync();

        CameraManager.instance.SetCameraPosTarget(null);

        zhangLiao.MoveSpeedMultiple(2f);
        guanYu.MoveSpeedMultiple(2f);
        zhangLiao.move.MoveToPointAdd(Vector3.left * 10, false);
        guanYu.move.MoveToPointAdd(Vector3.left * 10, false);

        await PopupManager.instance.ShowDimmAsync(true, _duration: 1);
    }

    async UniTask SecondPhaseAsync()
    {
        var guanYu = GetHero(CharacterName.GuanYu);
        var xiahouDun = GetHero(CharacterName.XiahouDun);

        CameraManager.instance.SetCameraPosTarget(guanYu.cameraPos);
        PopupManager.instance.ShowDimm(false, _duration: 1);
        await guanYu.move.DashAsync(guanYu.position + Vector3.left);
        await WaitForSeconds(.5f);

        //관우	..
        //관우	"괴물치고는..        급소는 모두 피했군."
        guanYu.move.SetFlip(true);
        await TalkStartAsync(2);

        //관우	후훗..
        TalkAutoClose(2);
        var target = xiahouDun.position + Vector3.left * 3;
        target.y = guanYu.position.y;
        guanYu.move.MoveToPoint(target, false);
        await UniTask.WaitUntil(() => (guanYu.position - xiahouDun.position).sqrMagnitude < 36);

        //하후돈	으으.. 장군..
        guanYu.talkbox.SetActive(false);
        await TalkStartAsync(1, true);

        await UniTask.WaitUntil(() => guanYu.move.isMoving == false);

        PopupManager.instance.AlertShow("스토리를_완료했습니다.");
        await WaitForSeconds(1f);

        //관우	"몸을 추스리시오.        다녀와서 그대의 술잔을 받도록 하지."
        await TalkStartAsync();

        CameraManager.instance.SetCameraPosTarget(null);
        guanYu.move.Dash(guanYu.position + Vector3.left * 15f, 0);

        //하후돈	캬캬캬
        await TalkAutoCloseAsync(0, true);
        await UniTask.WaitUntil(() => xiahouDun.talkbox.isTyping == false);
    }
}
