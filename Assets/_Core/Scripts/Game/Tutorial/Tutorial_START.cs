using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial_START : TutorialBase
{
    public override async UniTask StartAsync(TutorialType _type)
    {
        Signal.instance.ActiveHUD.Emit(false);

        // 밑에 버튼 영역 켜주자
        List<Button> bottomButton = new();
        {
            var canvasBottom = Scene_Lobby.instance.canvas.transform.Find("Bottom");
            canvasBottom.gameObject.SetActive(true);
            var panelBottom = canvasBottom.Find("Panel");

            for (int i = 0; i < panelBottom.childCount; i++)
            {
                bottomButton.Add(panelBottom.GetChild(i).GetComponent<Button>());
                bottomButton[i].interactable = false;
            }
        }

        var talk = TableManager.scenarioTalk.GetTalk("TUTORIAL_START", true);
        var mainHero = TeamManager.instance.mainHero;
        mainHero.move.SetFlip(true);

        var enemy = m_elementBase.enemy.First();
        enemy.gameObject.SetActive(false);

        mainHero.buff.Add(BuffType.DEBUFF_NO_SKILL);

        CameraManager.instance.SetCameraPosTarget(mainHero.element.cameraPos);
        // 딤 꺼주자
        await PopupManager.instance.ShowDimmAsync(false);

        // 음??
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        enemy.gameObject.SetActive(true);
        enemy.anim.Play(CharacterAnimType.Attack);
        enemy.SetHeroData("");
        CameraManager.instance.SetCameraPosTarget(enemy.element.cameraPos, false);

        //얼빠지게 생긴 넘이다!! 죽여라!!
        await enemy.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        //"앞에 황건적이네. 어쩌지?"
        CameraManager.instance.SetCameraPosTarget(mainHero.element.cameraPos, false);
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);
        enemy.talkbox.SetActive(false);

        //데미지 안받게 
        var hashHero = mainHero.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);
        var hashEnemy = enemy.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);

        mainHero.move.MoveTarget(m_elementBase.enemy.First(), true);
        enemy.move.MoveTarget(mainHero, true);

        //"응? 적이 있으면 스스로 공격하는구나!!"
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        ControllerManager.instance.gameObject.SetActive(true);
        ControllerManager.instance.SetMoveActionArea(true, false);
        ControllerManager.instance.SetActive_Action(false);
        //"키보드 조작으로 내가 움직일 수 있을거 같은데?"
        mainHero.talkbox.Start(talk.Dequeue().talkArray);

        var prevPos = mainHero.transform.position;
        await UniTask.WaitUntil(() => (prevPos - mainHero.transform.position).sqrMagnitude > 2f);

        //"돌진과 공격을 사용해보자"
        ControllerManager.instance.SetActive_Action(true);
        ControllerManager.instance.DashTimerStartAsync().Forget();
        mainHero.talkbox.Start(talk.Dequeue().talkArray);

        bool isAttack = false, isDash = false;
        while (isAttack == false || isDash == false)
        {
            if (isAttack == false)
                isAttack = mainHero.attack.isAttack;
            if (isDash == false)
                isDash = mainHero.move.isDash;

            await UniTask.WaitForEndOfFrame();
        }

        enemy.target.SetTarget(null);
        enemy.Respawn(false);
        enemy.move.MoveTarget(mainHero, true);
        mainHero.move.MoveTarget(enemy, true);

        ControllerManager.instance.SetMoveActionArea(false);
        var heroInfo = Scene_Lobby.instance.canvas.transform.Find("HeroInfo");
        heroInfo.gameObject.SetActive(true);
        var hi = heroInfo.GetComponentsInChildren<HeroInfoComponent>();
        for (int i = 0; i < hi.Length; i++)
            hi[i].StartStage();
        Utils.SetActivePunch(heroInfo, true);
        mainHero.buff.RemoveAll(BuffType.DEBUFF_NO_SKILL);
        //영웅 스킬 사용
        await mainHero.talkbox.StartAsync(talk.Dequeue().talkArray);
        await UniTask.WaitUntil(() => mainHero.attack.isUseSkill);

        mainHero.buff.RemoveAll();
        enemy.buff.RemoveAll();

        mainHero.talkbox.Start(talk.Dequeue().talkArray);
        await UniTask.WaitUntil(() => enemy.isLive == false);

        //보상 나오는 연출 해줄거야.

        m_elementBase.arrows[0].gameObject.SetActive(true);

        //"연회권? 동료를 얻을 수 있으려나? 주막에 가보자."
        mainHero.talkbox.Start(talk.Dequeue().talkArray);

        // 하단 버튼활성화
        for (int i = 0; i < bottomButton.Count; i++)
            bottomButton[i].interactable = true;
        await SummonHeroAsync();

        await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.M));

        m_elementBase.arrows[0].gameObject.SetActive(true);
        // 딤 켜주자
        await PopupManager.instance.ShowDimmAsync(true);
    }

    async UniTask SummonHeroAsync()
    {
        var screen = LobbyScreenManager.instance.GetScreenSummon();

        screen.SetEnableRegion(RegionType.Shu);

        // 영웅을 뽑고, 스크린을 닫을 떄까지 기다린다.
        while (DataManager.userInfo.myHero.Count == 1 || LobbyScreenManager.instance.curScreen > LobbyScreenType.None)
            await UniTask.WaitForEndOfFrame();

        screen.SetEnableRegion();
    }
}
