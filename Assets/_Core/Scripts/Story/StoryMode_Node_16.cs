using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StoryMode_Node_16 : StoryModeBaseComponent
{
    protected override async UniTask StartAsync()
    {
        var caoCao = mainHero;
        var yanLiang = GetHero(CharacterName.YanLiang);
        var xiahoDun = GetHero(CharacterName.XiahouDun);
        var xiahoYuan = GetHero(CharacterName.XiahouYuan);
        var guanYu = GetHero(CharacterName.GuanYu);
        var zhangLiao = GetHero(CharacterName.ZhangLiao);
        var wenChou = GetHero(CharacterName.WenChou);
        var yuanShao = GetHero(CharacterName.YuanShao);
        var liuBei = GetHero(CharacterName.LiuBei);

        CameraManager.instance.SetCameraPosTarget(caoCao.cameraPos);

        await PopupManager.instance.ShowDimmAsync(false);

        #region 관우등장
        //조조	"..?"
        //조조	"뭐라고? 송헌과 위속은 전사하고,        그 서황마저 패퇴했다고 ? "
        await TalkStartAsync(2);

        //안량	"하하하! 조조놈 장수들은        어찌 이리 버러지들 뿐이더냐ㅋㅋ"
        CameraManager.instance.SetCameraPosTarget(yanLiang.cameraPos, false);
        await WaitForSeconds(.8f);

        yanLiang.anim.PlayAttack();
        await TalkStartAsync();

        //하후돈	"저 새끼가.. - -+"
        await TalkStartAsync();

        //하후돈	"맹덕! 아니 주공!         내가 가서 저놈의 모가지를 따오겠오!"
        xiahoDun.anim.PlayAttack();
        await TalkStartAsync();

        //하후연	"형님! 이미 사기가 바닥입니다.        무작정 나간다고 될 일이 아니오."
        xiahoYuan.move.MoveToPoint(xiahoYuan.position + Vector3.right, false);
        await TalkStartAsync();

        //관우 등장
        var targetPos = caoCao.position + Vector3.left * 4;
        targetPos.y -= 0.2f;
        guanYu.move.MoveToPoint(targetPos, false);

        //하후돈	"그럼 저 놈이 지꺼리는 걸        보고만 있으란 소리냐! ? "
        xiahoDun.move.SetFlip(false);
        await TalkStartAsync();

        //하후돈 "응??"
        TalkAutoClose(0);

        //조조	어찌한다..
        await TalkStartAsync();
        guanYu.MoveSpeedMultiple(2f);
        caoCao.talkbox.SetActive(true);

        await UniTask.WaitUntil(() => guanYu.move.isMoving == false);
        guanYu.MoveSpeedMultiple(.5f);
        caoCao.talkbox.SetActive(false);
        xiahoDun.talkbox.SetActive(false);

        //관우	곤란하신 일이 있으신가봅니다, 조공.
        caoCao.move.SetFlip(false);
        zhangLiao.move.SetFlip(false);
        await TalkStartAsync();

        //장료	와주셨군요, 관장군!
        await TalkStartAsync();

        //장료	"주군, 제가 관장군과 함께        급습해 보겠습니다."
        zhangLiao.move.MoveToPoint(zhangLiao.position + Vector3.right * .5f, false);
        xiahoDun.move.SetFlip(true);
        await TalkStartAsync();

        //조조	(공을 세우게 하고 싶진 않지만..)
        await TalkStartAsync();
        caoCao.move.SetFlip(true);

        //조조	"관공, 투구에 화려한 깃이 있는        장수가 안량이오."
        //조조 그대가 보기에는 어떻소?
        await TalkStartAsync(2);

        //targetPos = caoCao.position + Vector3.right * 4;
        //targetPos.y -= 0.2f;
        //await guanYu.move.MoveToPointAsync(targetPos, false);

        //관우	"그저 '나 죽여주시오'        하는 것 같습니다."
        caoCao.move.SetFlip(false);
        await TalkStartAsync();

        //조조	"역시 그대답소ㅋ 좋다!        관공과 장료는 바로 출진하시오!"
        caoCao.move.SetFlip(true);
        caoCao.anim.PlayAttack();
        await TalkStartAsync();

        // 관우와 장료가 다가간다.
        guanYu.move.MoveToPoint(yanLiang.position + Vector3.left * 5, false);
        var zTarget = yanLiang.position + Vector3.left * 5.5f;
        zTarget.y = zhangLiao.position.y;
        zhangLiao.move.MoveToPoint(zTarget, false);

        //guanYu.move.SetFlip(true);
        //guanYu.anim.PlayAttack();

        //zhangLiao.move.SetFlip(true);
        //zhangLiao.anim.PlayAttack();

        #endregion 관우등장

        //화면 전환해주자
        await PopupManager.instance.ShowDimmAsync(true);

        guanYu.move.MoveStop();
        zhangLiao.move.MoveStop();

        guanYu.position = caoCao.position + Vector3.right * 4;
        zTarget = caoCao.position + Vector3.right * 3.5f;
        zTarget.y = zhangLiao.position.y;
        zhangLiao.position = zTarget;

        await WaitForSeconds(1f);
        CameraManager.instance.SetCameraPosTarget(yanLiang.transform);
        await PopupManager.instance.ShowDimmAsync(false);

        #region 안량 참수

        //안량	"캬캬캬 애꾸놈 우라통 터지는게        여기까지 느껴지는구나ㅋㅋ"
        yanLiang.anim.PlayAttack();
        await TalkStartAsync();

        // 관우와 장료가 다가간다.
        guanYu.move.MoveToPoint(yanLiang.position + Vector3.left * 5, false);
        zTarget = yanLiang.position + Vector3.left * 5.5f;
        zTarget.y = zhangLiao.position.y;
        zhangLiao.move.MoveToPoint(zTarget, false);

        //안량	"응? 수염? 현덕공이        말한 자가 저자인가 ? "
        await TalkStartAsync();

        //장료	"조심하십시오. 원소진영내        최고의 무장입니다."
        await TalkStartAsync();

        //안량  "이보시오, 당신이 관운장이오?우리 진영에 지금.."
        await TalkAutoCloseAsync(0);
        await UniTask.WaitUntil(() => guanYu.move.isMoving == false);
        await UniTask.WaitUntil(() => yanLiang.talkbox.isTyping == false);
        await WaitPointerDown();

        // 관우 스킬 날려주자
        await guanYu.attack.RushAsync(yanLiang.position + Vector3.right * 3);

        //안량.. ?
        yanLiang.anim.Play(CharacterAnimType.Knockdown);
        await TalkStartAsync();

        //장료	..?
        await TalkStartAsync();

        #endregion 안량 참수

        await PopupManager.instance.ShowDimmAsync(true);
        await WaitForSeconds(.5f);

        CameraManager.instance.SetCameraPosTarget(caoCao.transform);

        guanYu.position = caoCao.position + Vector3.right * 4;
        guanYu.move.SetFlip(false);

        zTarget = caoCao.position + Vector3.right * 5;
        zTarget.y = zhangLiao.position.y;
        zhangLiao.position = zTarget;
        zhangLiao.move.SetFlip(false);

        wenChou.position = yanLiang.position;
        Destroy(yanLiang.gameObject);

        PopupManager.instance.ShowDimm(false);

        #region 관우칭찬
        //조조	"하하하! 역시 그대는        하늘이 내린 신장이오!"
        //관우 과찬이십니다.
        //하후돈 흥, 나였다면 기합만으로도..
        //하후연 에헴..형님 ?
        await TalkStartAsync(4);

        // 장료가 문추쪽을 확인한다.
        zhangLiao.move.MoveToPoint(zhangLiao.position + Vector3.right, false);

        //관우	..
        await TalkStartAsync();

        //장료	"주군! 지금 문추가        미쳐 날뛰고 있습니다!"
        await TalkStartAsync();
        #endregion 관우칭찬

        #region 문추 베기
        CameraManager.instance.SetCameraPosTarget(wenChou.cameraPos, false);
        await WaitForSeconds(1f);

        zhangLiao.move.MoveToPoint(zhangLiao.position + Vector3.left * 4, false);

        //문추	"안량을 벤 자가 누구더냐!        분명 무슨 암수를 썻을테지!"
        wenChou.anim.PlayAttack();
        await TalkStartAsync();

        //유비	..
        //유비 관우는 아닐것입니다.
        //원소 두고 보면 알 일이오.
        await TalkStartAsync(3);

        guanYu.move.MoveToPoint(wenChou.position + Vector3.left * 5, false);

        //문추 흥!대체 안량이 어떻게..
        TalkAutoClose(0);

        await UniTask.WaitUntil(() => guanYu.move.isMoving == false, cancellationToken: m_cts.Token);

        //문추  응 ?
        await TalkStartAsync();

        // 관우 스킬 날려주자
        wenChou.anim.PlayAttack();
        await guanYu.attack.RushAsync(wenChou.position + Vector3.right * 3.5f);

        //문추	아니!!
        wenChou.move.SetFlip(true);
        TalkAutoClose(0);

        //관우 ..
        await TalkStartAsync(1, true);

        await guanYu.move.MoveToPointAsync(wenChou.position + Vector3.right * 3, false);
        guanYu.anim.PlayAttack(true, true);

        //문추 넉백
        wenChou.anim.Play(CharacterAnimType.Knockdown);
        await wenChou.transform.DOMoveX(wenChou.position.x + -1, 0.2f);

        //문추	"헉헉.. 이건 내가 어찌 할 수        있는 자가 아니다.."
        await TalkStartAsync();
        wenChou.move.MoveToPointAdd(Vector2.left * .5f, _isAniPlay: false);
        //문추	일단 도망을..
        await TalkStartAsync();

        //관우	ㅉㅉ
        wenChou.move.MoveToPointAdd(Vector2.left * .5f, _isAniPlay: false);
        guanYu.move.MoveToPoint(guanYu.position + Vector3.left, false);
        await TalkStartAsync();

        // 관우 스킬 날려주자
        await guanYu.attack.RushAsync(wenChou.position + Vector3.left * 3f);

        //문추	으아!
        wenChou.anim.Play(CharacterAnimType.Die_1);
        await TalkStartAsync();

        guanYu.move.MoveToPoint(wenChou.position + Vector3.left * 2, false);

        #endregion 문추 베기

        //원소	"이이!! 저 자는 분명 관우지 않소!        어떻게 된것이오 현덕공!"
        yuanShao.move.MoveToPoint(yuanShao.position + Vector3.left, false);
        await TalkStartAsync();

        //유비	"명공! 조조는 원래 저를 시기하여        명공의 손을 빌려 저를 죽이려는 것입니다."
        //원소 으으 - -+
        yuanShao.move.SetFlip(true);
        liuBei.move.MoveToPoint(liuBei.position + Vector3.right * .5f);
        await TalkStartAsync(2);

        //관우	흐음..
        //guanYu.move.SetFlip(true);
        await TalkStartAsync();

        //질문창 띄워주자
        {
            //관우	"이 정도면 빚은 갚은거겠지.            이제 돌아가볼까?"
            //관우	"애꾸놈의 기를 확실히 꺽어주지.            이 자의 목을 걸어두고 함성을 질러라!!"
            m_resultTalkIdx = await PopupManager.instance.OpenTalkSelectAsync(
                m_queTalk.Dequeue().message,
                m_queTalk.Dequeue().message);

            m_queTalk = new(
                TableManager.scenarioTalk.GetTalkAfterQuestion(DataManager.storyMode.curNodeKey, m_resultTalkIdx));

            if (m_resultTalkIdx == 1)
            {
                //유비	"관우는 내가 여기 있는                것을 모를 것입니다."
                //유비  "제가 직접 관우에게 밀사를 보내겠습니다.   그렇다면 관우는 바로 달려올 것입니다."
                //원소..
                //유비  "안량, 문추를 잃은 것은 안타까운 일입니다."
                //유비  "허나, 관우를 얻는 것은 천하를    얻는 것과 다름이 없을 것입니다!"
                //원소  "하하하. 내가 뭐 그리 소인배도 아니고ㅋㅋ"
                await TalkStartAsync(6);

                await yuanShao.move.MoveToPointAsync(yuanShao.position + Vector3.left, false);

                //원소	"오늘 밤 관우를 맞이하는                술잔을 기울이도록 하지ㅋ"
                await TalkStartAsync();

                liuBei.move.MoveToPointAdd(Vector2.left * .5f);
            }
            else
            {
                guanYu.anim.PlayAttack(true, true);
                await WaitForSeconds(.5f);

                var lookAt = (liuBei.position - yuanShao.position).normalized;

                //원소	..
                //유비    ..관우야 ??
                await TalkStartAsync(2);

                //원소  "유비.. 네 놈 때문에 내가 제일 아끼는   장수 두명을 잃었다."
                //유비  "아니 죽인 것은 관우인데요?      그게 아니라. 제가 지금 당장 밀서를.."
                yuanShao.move.MoveToPointAdd(lookAt * .3f);
                await TalkStartAsync(2);

                //원소 아니오.
                //유비  "안량, 문추를 잃은 것은    안타까운.."
                yuanShao.move.MoveToPointAdd(lookAt * .3f);
                await TalkStartAsync(2);

                //원소  "자네의 목으로 그들의      원한을 풀어주겠소."
                //유비  "저를 살려주시면     관우를 얻게 될 것입니다!"
                await TalkStartAsync(2);

                //원소	"가서 안량과 문추에게                안부나 전해주시게."
                yuanShao.move.MoveToPointAdd(lookAt * .3f);
                yuanShao.attack.SetActive_Weapon(true);
                await TalkStartAsync();

                yuanShao.anim.PlayAttack(true, true);
                liuBei.anim.Play(CharacterAnimType.Die_1);
                //LiuBei	유비	으윽..
                await TalkStartAsync();
            }

            PopupManager.instance.AlertShow("스토리를_완료했습니다.");

            //1.유비	    관우.. 살아 있었구나..
            //2.관우      흐음.. 갑자기 형님이 그립군..
            await TalkStartAsync();

            //guanYu.anim.PlayAttack(true);
        }
    }
}
