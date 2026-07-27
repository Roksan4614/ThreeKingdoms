using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine;

public class StoryMode_Node_28 : StoryModeBaseComponent
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

		xiahouDun.move.SetFlip(false);

		//관우	"자아. 빠르게 진격하겠소.        전군, 출진하라!"
		guanYu.anim.PlayAttack();
		await TalkStartAsync();


		//장료	오오!!
		TalkAutoClose(0);
		zhangLiao.anim.PlayAttack();

		//하후돈	..
		TalkAutoClose(0);

		await WaitPointerDown();
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
		zhangFei.SetTalkboxName("??");

		CameraManager.instance.SetCameraPosTarget(enemy.cameraPos);

		await PopupManager.instance.ShowDimmAsync(false);

		//Enemy	으윽.. 이 악귀놈..
		enemy.move.MoveToPointAdd(Vector2.left * .5f);
		await TalkStartAsync();

		//guanYu.move.MoveToPointAdd(Vector2.left);

		//Enemy	으아악!!
		enemy.anim.AttackMotionFirstFrame();
		TalkAutoClose(0);

		enemy.MoveSpeedMultiple(4f);
		enemy.move.MoveToPoint(guanYu.position, _isAniPlay: false);
		await UniTask.WaitUntil(() => (enemy.position - guanYu.position).sqrMagnitude < 36);
		enemy.move.MoveStop();
		enemy.anim.animSpeed = 1f;
		enemy.move.MoveToPointAdd(Vector2.left, _isAniPlay: false);
		enemy.anim.Play(CharacterAnimType.Die_1);
		enemy.talkbox.SetActive(false);

		// 관우 돌격
		await guanYu.attack.RushAsync(enemy.position + Vector3.left * 5f);

		//Enemy	으으..
		TalkAutoClose();

		await WaitForSeconds(1f);

		//관우	악귀..라..
		guanYu.move.SetFlip(true);
		TalkAutoClose(0);

		{
			var target = guanYu.position + Vector3.right * 4.5f;
			target.y = xiahouDun.position.y;

			xiahouDun.move.MoveToPoint(target);

			target.x -= .5f;
			target.y = zhangLiao.position.y;

			await zhangLiao.move.DashAsync(target, 0);
		}

		await UniTask.WaitUntil(() => guanYu.talkbox.isTyping == false);
		await WaitPointerDown();
		guanYu.talkbox.SetActive(false);
		enemy.talkbox.SetActive(false);

		//장료	"끝이군요. 이로써        형주까지 우리 수중에.."
		TalkAutoClose(0);
		await WaitForSeconds(1f);

		//관우	..?
		guanYu.move.SetFlip(false);
		await TalkStartAsync(1, false);

		guanYu.move.MoveToPoint(etc.position + Vector3.right * 5f);

		//장료 장군?
		await TalkStartAsync();

		CameraManager.instance.SetCameraPosTarget(guanYu.cameraPos, false);
		await UniTask.WaitUntil(() => guanYu.move.isMoving == false);

		//Etc	사..살려주세요ㅜㅜ
		etc.move.MoveToPointAdd(Vector2.left * .5f);
		await TalkStartAsync(1, false);

		{
			var target = guanYu.position + Vector3.right * 3.5f;

			target.y = zhangLiao.position.y;
			zhangLiao.move.MoveToPoint(target);

			target.y = xiahouDun.position.y;
			target.x -= .5f;

			await xiahouDun.move.DashAsync(target, 0);
		}

		//하후돈	"뭐하는거요, 장군.            그는 일반 백성일 뿐이오."
		guanYu.move.MoveToPointAdd(Vector2.left * 2f);
		etc.move.MoveToPointAdd(Vector2.left * .5f);
		await TalkStartAsync();

		//관우	"후훗.. 서주가 떠올라 흥분되오?            나 역시 그렇군ㅋㅋ"
		guanYu.move.MoveToPointAdd(Vector2.left);
		etc.move.MoveToPointAdd(Vector2.left * .5f);
		await TalkStartAsync();

		guanYu.anim.AttackMotionFirstFrame(CharacterAnimType.Attack_Move, 1);
		guanYu.move.MoveToPointAdd(Vector2.left * .5f, _isAniPlay: false);

		//하후돈	그만두라고 이 자식아!!
		xiahouDun.MoveSpeedMultiple(2f);
		xiahouDun.move.MoveToPointAdd(Vector2.left * 2);
		await TalkStartAsync();
		xiahouDun.MoveSpeedMultiple(.5f);

		//장료	!!
		TalkAutoClose(0);

		zhangFei.move.Dash(etc.position + Vector3.left * 5, 0, .2f);

		await UniTask.WaitUntil(() => (guanYu.position - zhangFei.position).sqrMagnitude < 16);
		guanYu.move.MoveStop();
		guanYu.move.SetFlip(true);
		guanYu.move.MoveToPointAdd(Vector2.left, _isAniPlay: false);
		guanYu.anim.animSpeed = 1f;
		guanYu.anim.PlayAttack(true, true);

		// 백성 뒤로 밀리기
		while (zhangFei.move.isDash)
		{
			var pos = zhangFei.position + Vector3.left * 2;
			pos.x = Mathf.Min(etc.position.x, pos.x);

			etc.position = pos;

			await UniTask.NextFrame();
		}

		//관우	음??
		await TalkStartAsync();
		zhangLiao.talkbox.SetActive(false);
		guanYu.move.SetFlip(false);

		//??	흥!!
		await TalkStartAsync(1, false);

		zhangFei.move.SetFlip(true);
		await WaitForSeconds(1f);

		//??	못봐주겠구만ㅋ
		etc.move.MoveToPointAdd(Vector2.left * .5f);
		zhangFei.move.MoveToPointAdd(Vector2.right);
		await TalkStartAsync();

		etc.MoveSpeedMultiple(2f);
		etc.element.collider.enabled = false;
		etc.move.MoveToPointAdd(Vector2.left * 10, false);

		//관우	..?
		await TalkStartAsync();

		xiahouDun.move.MoveToPointAdd(Vector3.left * .5f);
		await zhangLiao.move.DashAsync(zhangLiao.position + Vector3.left * 4, 0, .2f);

		//하후돈	..
		TalkAutoClose(0);

		//장료	웬놈이냐!!
		await TalkStartAsync();
		xiahouDun.talkbox.SetActive(false);

		//??	"모든 전장을 피로 물들이더니,        이제 백성까지 공격하는 것이냐!!"
		await TalkStartAsync();

		//관우	"어차피 살려줘봤자 나중에는        복수한답시고 달려들테지."
		await TalkStartAsync();

		//관우  "미리 싹을 없애버리는 것 또한     전술 중 일부일 뿐이라네ㅋㅋ"
		guanYu.anim.PlayAttack(true, true);
		guanYu.move.MoveToPointAdd(Vector3.left * .5f, _isAniPlay: false);
		await TalkStartAsync();

		//??	".."
		//??	"어이가 없군ㅋㅋ        악귀가 따로 없지 않은가ㅋ"
		await TalkStartAsync(2, false);

		zhangFei.move.SetFlip(false);
		await WaitForSeconds(1f);
		zhangFei.anim.PlayAttack(true, true);

		//??	"흥! 아직 내가 나설 수        없음을 감사히 여기거라"
		await TalkStartAsync(1, false);

		//??	3년상만 아니었어도.. 에잇!!
		zhangFei.move.MoveToPointAdd(Vector3.left * .5f);
		await TalkStartAsync();

		zhangFei.element.collider.enabled = false;
		zhangFei.move.Dash(zhangFei.position + Vector3.left, 15);

		await WaitForSeconds(1f);

		//관우	..
		TalkAutoClose(0);

		xiahouDun.move.MoveToPointAdd(Vector2.left * .5f);
		//하후돈	"멧돼지 같은게,        엄청 빠르군."
		xiahouDun.talkbox.isSwitch_IgnoreRoundScreen = true;
		TalkAutoClose(0, false);

		PopupManager.instance.AlertShow("스토리를_완료했습니다.");
		await WaitPointerDown();

	}
}
