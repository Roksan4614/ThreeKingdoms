using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class StoryMode_Node_30b : StoryModeBaseComponent
{
    protected async override UniTask StartAsync()
    {
        await Phase_First();

        await SetNextPhaseAsync();

        await Phase_Second();
    }

    async UniTask Phase_First()
    {
        var token = m_cts.Token;

        var liuBei = mainHero;
        var guanYu = phase.GetHero(CharacterName.GuanYu);
        var zhangFei = phase.GetHero(CharacterName.ZhangFei);

        CameraManager.instance.SetCameraPosTarget(liuBei.cameraPos);

        await PopupManager.instance.ShowDimmAsync(false);

        // .. 황건적을 토벌 할 의용군을 모집한다??
        await TalkStartAsync();
        // 에휴..
        TalkAutoClose();

        // 장비가 다가온다.
        await zhangFei.move.MoveToPointAsync(zhangFei.position + Vector3.right * 4);
        await WaitPointerDown();

        // 장비	사내가 먼 한숨이오?? 한심하기는!
        liuBei.move.SetFlip(false);
        liuBei.talkbox.SetActive(false);
        await TalkStartAsync();

        var targetGuanYu = guanYu.position + Vector3.right * 9;
        // 관우 등장
        guanYu.move.MoveToPoint(guanYu.position + Vector3.right * 6);

        // 유비	음.. 의용군에게는 뛰어난 지도자가 필요하오.
        liuBei.talkbox.SetActive(false);
        TalkAutoClose(0);
        await UniTask.WaitUntil(() => guanYu.move.isMoving == false);

        //관우 ??
        isLock_MoveCamera = true;
        TalkAutoClose(0);
        await WaitPointerDown();

        // 유비	지도자 없는 의용군은 오합지졸일 뿐..
        await TalkStartAsync();

        // 관우	그렇다면 그대가 지도자가 되면 될 거 아니오.
        guanYu.move.MoveToPoint(targetGuanYu);
        //isLock_MoveCamera = true;
        await TalkStartAsync();

        // 유비 나에게는 그럴 힘이 없소.
        liuBei.move.SetFlip(true);
        liuBei.move.MoveToPoint(liuBei.position + Vector3.right);
        await TalkStartAsync();

        // 장비 흥!내가 도와주겠소이다! 어디 가서 술이나 한잔 합시다.
        liuBei.move.SetFlip(false);
        zhangFei.anim.PlayAttack();
        await TalkStartAsync();

        // 유비 그럼 내가 좋은 곳을 알고 있으니, 따라 오시겠오?
        // 관우 흠흠..나도 함께 가지.
        liuBei.move.MoveToPoint(liuBei.position + Vector3.left);
        await TalkStartAsync(2);

        await PopupManager.instance.ShowDimmAsync(true);
    }
    async UniTask Phase_Second()
    {
        var liuBei = mainHero;
        var guanYu = phase.GetHero(CharacterName.GuanYu);
        var zhangFei = phase.GetHero(CharacterName.ZhangFei);

        CameraManager.instance.SetCameraPosTarget(zhangFei.cameraPos);
        await PopupManager.instance.ShowDimmAsync(false);

        // 장비 오오!복숭아 밭 파티장이라니ㅋ 근사하구만ㅋ
        isLock_MoveCamera = true;
        CameraManager.instance.SetCameraPosTarget(null);
        TalkAutoClose(0);

        // 장비 왔다갔다하자.
        await zhangFei.move.MoveToPointAsync(zhangFei.position + Vector3.right * 2, false);
        await zhangFei.move.MoveToPointAsync(zhangFei.position + Vector3.left * 2, false);

        await WaitPointerDown();
        zhangFei.talkbox.SetActive(false);

        // 관우  없는 살림에 너무 무리하시는게 아닐런지..
        await TalkStartAsync();
        zhangFei.move.SetFlip(true);

        //질문창 띄워주자
        {
            // 음...
            liuBei.move.SetFlip(false);
            await TalkStartAsync();

            //  "한왕실의 재건을 위해 그대들을 맞이하는 것이니 그런소리 마시오."
            //  "무슨 소리신지, 이 근방 돗자리는 모두 내가 납품하고 있소만."
            var resultIdx = await PopupManager.instance.OpenTalkSelectAsync(
                m_queTalk.Dequeue().message,
                m_queTalk.Dequeue().message);

            Queue<TableStringData> talkQueston = new(
                TableManager.scenarioTalk.GetTalkAfterQuestion(DataManager.storyMode.curNodeKey, resultIdx));

            // 장비 역시 큰 뜻을 품고 있었구려ㅋ
            // 관우  그 뜻에 동참하겠소. 형님으로 모시겠습니다.

            // 장비 아니!!부자였소 ?? 그런데 왜 그런 거지꼴로 다녔던거요??
            // 유비  뭐 딱히..
            // 관우  하하하! 좋소이다! 큰 형님으로 모실테니 큰 뜻을 품어주십시오ㅋ

            while (talkQueston.Count > 0)
                await TalkStartAsync(talkQueston.Dequeue());
        }

        PopupManager.instance.AlertShow("관우와_장비가_진영에_합류했습니다.");

        // 유비  고맙소. 태어난 날은 다르나 한날 한시에 죽을 것을 맹세합시다.
        await TalkStartAsync();

        // 장비 응?? 그건 좀..
        TalkAutoClose(0);
        await UniTask.WaitUntil(() => zhangFei.talkbox.isTyping == true);
        await UniTask.WaitUntil(() => zhangFei.talkbox.isTyping == false);

        // 관우 뜨거운 맹세! 영원히 변치 않을 것입니다!
        isLock_MoveCamera = true;
        TalkAutoClose(0);

        await WaitForSeconds(1);

        // 장비  않을 것이오!!
        TalkAutoClose(0);
        await UniTask.NextFrame();
        await UniTask.WaitUntil(() => zhangFei.talkbox.isTyping == true);
        await UniTask.WaitUntil(() => zhangFei.talkbox.isTyping == false);

        await WaitForSeconds(.5f);

        PopupManager.instance.AlertShow("스토리를_완료했습니다.");
        await WaitPointerDown();

    }
}
