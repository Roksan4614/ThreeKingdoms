using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial_START : TutorialBase
{
    private void Update()
    {
        if (Input.GetKey(KeyCode.K))
            RewardWorker.instance.isSwitchReceive = true;
    }

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

        {
            var resultIdx = await PopupManager.instance.OpenTalkSelectAsync(
                "튜토리얼 진행할거야.",
                "일단 멈추고 개발할거야."
                );

            if (resultIdx == 1)
            {
                ControllerManager.instance.DashTimerStartAsync().Forget();
                // 하단 버튼활성화
                for (int i = 0; i < bottomButton.Count; i++)
                    bottomButton[i].interactable = true;

                ControllerManager.instance.gameObject.SetActive(true);
                while (true)
                {
                    await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.L));

                    RewardWorker.instance.isSwitchReceive = false;
                    RewardWorker.instance.Run(enemy.transform.position,
                        ItemType.Gold + UnityEngine.Random.Range(0, (int)ItemType.MAX - 1));

                    await UniTask.WaitForEndOfFrame();
                }
            }
        }

        CameraManager.instance.SetCameraPosTarget(enemy.element.cameraPos, false);

        // 얼빠지게 생긴 넘이다!! 죽여라!!
        await enemy.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        CameraManager.instance.SetCameraPosTarget(mainHero.element.cameraPos, false);

        // "앞에 황건적이네. 어쩌지?"
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);
        enemy.talkbox.SetActive(false);

        // 데미지 안받게 
        var hashHero = mainHero.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);
        var hashEnemy = enemy.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);

        mainHero.move.MoveTarget(m_elementBase.enemy.First(), true);
        enemy.move.MoveTarget(mainHero, true);

        // "응? 적이 있으면 스스로 공격하는구나!!"
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        ControllerManager.instance.gameObject.SetActive(true);
        ControllerManager.instance.SetMoveActionArea(true, false);
        ControllerManager.instance.SetActive_Action(false);
        // "키보드 조작으로 내가 움직일 수 있을거 같은데?"
        mainHero.talkbox.Start(talk.Dequeue().talkArray);

        var prevPos = mainHero.transform.position;
        await UniTask.WaitUntil(() => (prevPos - mainHero.transform.position).sqrMagnitude > 2f);

        // "돌진과 공격을 사용해보자"
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
        Utils.SetActivePunch(heroInfo, true);

        var hi = heroInfo.GetComponentsInChildren<HeroInfoComponent>();
        for (int i = 0; i < hi.Length; i++)
            hi[i].StartStage();
        mainHero.buff.RemoveAll(BuffType.DEBUFF_NO_SKILL);
        // 영웅 스킬 사용
        await mainHero.talkbox.StartAsync(talk.Dequeue().talkArray);
        await UniTask.WaitUntil(() => mainHero.attack.isUseSkill);

        mainHero.buff.RemoveAll();
        enemy.buff.RemoveAll();

        mainHero.talkbox.Start(talk.Dequeue().talkArray);
        await UniTask.WaitUntil(() => enemy.isLive == false);

        var rtArrow = (RectTransform)m_elementBase.arrows[0].transform;

        // 연회 열기
        if (DataManager.userInfo.myHero.Count == 1)
        {
            bottomButton[(int)LobbyScreenType.Summon].interactable = true;

            // 연회권 보상 연출
            await RewardWorker.instance.RunAsync(enemy.transform.position, ItemType.Scroll_Party);

            await UniTask.WaitForSeconds(.5f);

            // "연회권? 동료를 얻을 수 있으려나? 주막에 가보자."
            await mainHero.talkbox.StartAsync(talk.Dequeue().talkArray);

            await UniTask.WaitForSeconds(.5f);

            rtArrow.gameObject.SetActive(true);
            // 영웅 뽑기
            await SummonHeroAsync();
            rtArrow.gameObject.SetActive(false);
        }
        else
            talk.Dequeue();

        // "영웅을 출전시켜보자."
        {
            await mainHero.talkbox.StartAsync(talk.Dequeue().talkArray);
            await UniTask.WaitForSeconds(.5f);

            bottomButton[(int)LobbyScreenType.Summon].interactable = false;
            bottomButton[(int)LobbyScreenType.Heros].interactable = true;

            rtArrow.anchoredPosition += new Vector2(-200, 0);
            rtArrow.gameObject.SetActive(true);

            bool isRetry = false;
            while (true)
            {
                // 영웅 창 기다리기
                await UniTask.WaitUntil(() => LobbyScreenManager.instance.curScreen == LobbyScreenType.Heros);

                // 꺼질 때가지 기다리기
                // 영웅 창 기다리기
                await UniTask.WaitUntil(() => LobbyScreenManager.instance.curScreen != LobbyScreenType.Heros);

                //배치 영웅 세명 검색
                if (DataManager.userInfo.myHero.Count(x => x.isBatch) == 3)
                {
                    if (isRetry == false)
                        talk.Dequeue();
                    break;
                }

                if (isRetry == false)
                {
                    // 영웅 얼굴을 누르면 합류시킬 수 있어.
                    mainHero.talkbox.Start(talk.Dequeue().talkArray);
                    isRetry = true;
                }
            }

            rtArrow.gameObject.SetActive(false);
            enemy.gameObject.SetActive(false);
        }

        TutorialManager.instance.Complete(TutorialType.START);

        // 자아! 이제 출발이다!!
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        await UniTask.WaitForSeconds(0.5f);

        for (int i = 0; i < bottomButton.Count; i++)
            bottomButton[i].interactable = true;

        // 딤 켜주자
        await PopupManager.instance.ShowDimmAsync(true);
    }

    async UniTask SummonHeroAsync()
    {
        var screen = LobbyScreenManager.instance.GetScreenSummon();
        screen.SetRegionType(TeamManager.instance.mainHero.data.regionType);

        bool isHeroSummon = false;

        // 영웅을 뽑고, 스크린을 닫을 떄까지 기다린다.
        while (DataManager.userInfo.myHero.Count == 1 || LobbyScreenManager.instance.curScreen > LobbyScreenType.None)
        {
            if (isHeroSummon == false && DataManager.userInfo.myHero.Count > 1)
            {
                m_elementBase.arrows[0].gameObject.SetActive(false);
                isHeroSummon = true;
            }

            await UniTask.WaitForEndOfFrame();
        }

        screen.SetRegionType(RegionType.NONE);
    }
}
