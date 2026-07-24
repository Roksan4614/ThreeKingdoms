using Cysharp.Threading.Tasks;
using UnityEngine;

public class StoryMode_Node_27 : StoryModeBaseComponent
{
    protected override async UniTask StartAsync()
    {
        var guanYu = GetHero(CharacterName.GuanYu);
        var caoCao = GetHero(CharacterName.CaoCao);
        var zhangLiao = GetHero(CharacterName.ZhangLiao);
        var xunYu = GetHero(CharacterName.XunYu);

        CameraManager.instance.SetCameraPosTarget(zhangLiao.cameraPos);

        await PopupManager.instance.ShowDimmAsync(false);

        //관우	뭐라고? 방금 뭐라고 하셨소!?
        guanYu.move.MoveToPointAdd(Vector3.left * .5f);
        await TalkStartAsync(1, true);

        //장료	"원소군에 의탁해 있던 현덕공이        원소의 의해 처형당했다 합니다."
        await TalkStartAsync();

        //관우	"서..설마.. 내가 안량과 문추를        베어서..인가 ?? "
        await TalkStartAsync(1, true);

        //관우	흑흑.. 형님.. ㅜㅜ
        guanYu.anim.Play(CharacterAnimType.Frust);
                await TalkStartAsync(1, true);

        //관우	"한날 한시에 죽겠다는 맹세,        결코 헛되지 않을 것이오!"
        guanYu.anim.Play(CharacterAnimType.Idle);
        guanYu.attack.SetActive_Weapon(true);
        guanYu.move.SetFlip(false);
        TalkAutoClose(0, true);

        await WaitForSeconds(1f);

        //장료	앗!!
        TalkAutoClose(0, true);
        zhangLiao.MoveSpeedMultiple(2f);
        zhangLiao.move.MoveToPointAdd(Vector2.left * .5f);

        // 관우 대사 치고 클릭할 때까지 기다리기
        await UniTask.WaitUntil(() => guanYu.talkbox.isTyping == false);
        await WaitPointerDown();

        zhangLiao.MoveSpeedMultiple(.5f);
        zhangLiao.talkbox.SetActive(false);
        zhangLiao.move.SetFlip(true);

        //조조 이동. 이속 증가
        caoCao.MoveSpeedMultiple(2f);
        caoCao.move.MoveToPoint(zhangLiao.position);

        //조조	관공!!
        CameraManager.instance.SetCameraPosTarget(null);
        TalkAutoClose(0, true);

        await UniTask.WaitUntil(() => (zhangLiao.position - caoCao.position).sqrMagnitude < 8);

        {
            var target = zhangLiao.position + new Vector3(2, 3);
            zhangLiao.move.MoveToPoint(target, _onComplete: () => zhangLiao.move.SetFlip(false));
        }

        await UniTask.WaitUntil(() => caoCao.move.isMoving == false);
        caoCao.MoveSpeedMultiple(.5f);

        await WaitPointerDown();
        guanYu.talkbox.SetActive(false);

        //조조	어찌 이리 어리석은!
        await TalkStartAsync();

        //관우	"조공. 그대에겐 이미 공을 세워        빚을 다 갚았다 생각하오."
        //관우  "그대는 그대의 일을 하시오."
        //조조	어찌 그런 소리를 하시는거요?
        //관우  "유비 형님이 안계신 이상    한왕실의 운명도 여기까지요."
        await TalkStartAsync(4, true);

        //조조	"나를 도와 한왕실을 부흥을 시키고,        천하를 안정토록 하면 될 것 아니오?"
        caoCao.move.MoveToPointAdd(Vector3.left * .5f);
        await TalkStartAsync();

        //관우	"조공, 그대에게 한왕실은        그저 도구일 뿐이지 않소."
        await TalkStartAsync(1, true);

        //조조	그..그건..
        TalkAutoClose(0);

        {
            var target = caoCao.position + Vector3.right;
            target.y = xunYu.position.y;
            xunYu.MoveSpeedMultiple(2f);
            xunYu.move.MoveToPoint(target);
        }

        await WaitForSeconds(1f);
        await UniTask.WaitUntil(() => caoCao.talkbox.isTyping == false);
        caoCao.talkbox.SetActive(false);

        CameraManager.instance.SetCameraPosTarget(null);
        caoCao.move.SetFlip(true);

        //순욱	관장군! 말씀이 너무 지나치시오!
        TalkAutoClose(0, true);
        await UniTask.WaitUntil(() => xunYu.move.isMoving == false);
        caoCao.talkbox.SetActive(false);

        xunYu.MoveSpeedMultiple(.5f);
        await WaitPointerDown();

        //순욱	"우리 주군께선 그런        파렴치한 자가 아니오!"
        guanYu.attack.SetActive_Weapon(false);
        guanYu.move.SetFlip(true);
        await TalkStartAsync();

        //조조	.. 저기..
        await TalkStartAsync(1, true);

        caoCao.move.SetFlip(false);
        xunYu.move.MoveToPointAdd(Vector3.left * 2);
        //순욱	"만약 한왕실의 녹을 받으면서        한치라도 그런 마음을 먹는다면    천벌을 받아 마땅할 것이오!!"
        await TalkStartAsync();

        //순욱	그렇지 않습니까, 주군.
        xunYu.move.SetFlip(true);
        await TalkStartAsync(1, true);

        //조조	 - -+
        await TalkAutoCloseAsync(0, true);
        await UniTask.WaitUntil(() => caoCao.talkbox.isTyping == false);

        //순욱	..??
        TalkAutoClose(0, true);
        await WaitPointerDown();

        caoCao.move.MoveToPointAdd(Vector2.left);
        xunYu.move.MoveToPointAdd(Vector3.right * 2);
        xunYu.move.SetFlip(false);
        xunYu.talkbox.SetActive(false);

        //조조	"관공! 내가 유비의 장례는        최고의 예를 다해 모셔주겠소."
        await TalkStartAsync();

        //관우	..
        //조조	"유비의 두 부인 또한 극진히        모실 것을 내 약속하겠소."
        //관우	"조공.. 그대는 진심으로 한왕실을 위하고,        백성을 먼저 생각하는 길을 갈 수 있겠소 ? "
        await TalkStartAsync(3, true);

        //조조	그..그건..
        caoCao.move.MoveToPointAdd(Vector2.right * .5f);
        await TalkStartAsync(1, true);

        //관우	"형님께서 바라시던 바는 그 것 뿐이었소.        그 것만 약조해준다면.."
        guanYu.move.MoveToPointAdd(Vector3.left * .5f, false);
        await TalkStartAsync();

        //관우	"이 관운장! 지금 당장         그대의 칼이 되어 주겠소!"
        guanYu.anim.PlayAttack(true, true);
        await TalkStartAsync();

        //조조	"좋소! 나 조맹덕!         관공과 하늘 앞에 맹세하겠소이다!"
        guanYu.move.SetFlip(true);
        await TalkStartAsync();
        //조조  "도탄에 빠진 백성을 구하고,  한 왕실의 부흥을 위해 목숨을 바치겠소!"
        TalkAutoClose(0);

        await WaitForSeconds(1f);

        //순욱	오오!!
        TalkAutoClose(0, true);

        await WaitForSeconds(.5f);

        await UniTask.WaitUntil(() => caoCao.talkbox.isTyping == false);
        await WaitPointerDown();
        caoCao.talkbox.SetActive(false);
        xunYu.talkbox.SetActive(false);
        zhangLiao.talkbox.SetActive(false);

        //장료	음?? 갑자기 번개가..?
        zhangLiao.move.MoveToPointAdd(Vector3.right, false);
        TalkAutoClose(0, true);

        //관우	..
        await TalkStartAsync();
        zhangLiao.talkbox.SetActive(false);
        zhangLiao.move.MoveToPointAdd(Vector3.left, false);

        //관우	"조공, 마지막으로 부탁이 있소.        내게 병력을 내어주시오."
        //조조 병력을.. ?
        //관우  "우선 원소의 모가지에서 흐르는 피로  형님의 넋을 달래주어야겠소."
        await TalkStartAsync(3, true);

        //장료; ;
        TalkAutoClose(0, true);

        //조조 그..그렇게 하시오..
        await TalkStartAsync(1, true);

        PopupManager.instance.AlertShow("스토리를_완료했습니다.");

        {
            // 조금 아래로 갔다가 오른쪽으로 빠져주자. 조조 앞으로 나오기 위해
            var target = caoCao.position;
            target.x -= 2f;
            target.y += 0.1f;
            await guanYu.move.MoveToPointAsync(target);
            guanYu.move.MoveToPointAdd(Vector2.right * 100);
            CameraManager.instance.SetCameraPosTarget(guanYu.cameraPos, false);
        }

        while (caoCao.position.x > guanYu.position.x || zhangLiao.position.x > guanYu.position.x || xunYu.position.x > guanYu.position.x)
        {
            if (caoCao.position.x < guanYu.position.x && caoCao.move.isFlip == false)
                caoCao.move.SetFlip(true);

            if (zhangLiao.position.x < guanYu.position.x && zhangLiao.move.isFlip == false)
                zhangLiao.move.SetFlip(true);

            if (xunYu.position.x < guanYu.position.x && xunYu.move.isFlip == false)
                xunYu.move.SetFlip(true);

            await UniTask.NextFrame(cancellationToken: m_cts.Token);
        }

        await PopupManager.instance.ShowDimmAsync(true, _duration: 1f);

        guanYu.move.MoveStop();
    }
}
