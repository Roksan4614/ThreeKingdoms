using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial_START : TutorialBase
{
    CancellationTokenSource m_cts;

    public override async UniTask StartAsync(TutorialType _type)
    {
        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;

        BannerComponent.instance.AddListenerSkip(() => OnButtonAsync_Skip().Forget());

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

#if !UNITY_EDITOR && UNITY_WEBGL
        var talk = TableManager.scenarioTalk.GetTalk("TUTORIAL_START", MessageHandler.IsMobileBrowser() == false);
#else
        var talk = TableManager.scenarioTalk.GetTalk("TUTORIAL_START", true);
#endif
        var mainHero = TeamManager.instance.mainHero;
        mainHero.move.SetFlip(true);

        var enemy = m_elementBase.enemy.First();
        // 이걸 넣어줘야 스킬쓰거나 할 때 정상작동함
        StageManager.instance.AddEnemyList(enemy);
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

        // 데미지 안받게 
        var hashHero = mainHero.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);
        var hashEnemy = enemy.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);

#if UNITY_EDITOR
        {
            var resultIdx = await PopupManager.instance.OpenTalkSelectAsync(
                "튜토리얼 진행할거야.",
                "일단 멈추고 개발할거야."
                );

            if (resultIdx == 2)
            {
                ControllerManager.instance.DashTimerStartAsync().Forget();
                // 하단 버튼활성화
                for (int i = 0; i < bottomButton.Count; i++)
                    bottomButton[i].interactable = true;

                ControllerManager.instance.gameObject.SetActive(true);
                while (true)
                {
                    await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.L), cancellationToken: token);

                    RewardWorker.instance.isSwitchReceive = false;
                    RewardWorker.instance.Run(enemy.transform.position,
                        ItemType.Gold + UnityEngine.Random.Range(0, (int)ItemType.MAX - 1), _durationWait: 2f);

                    await UniTask.NextFrame(cancellationToken: token);
                }
            }
        }
#endif

        CameraManager.instance.SetCameraPosTarget(enemy.element.cameraPos, false);

        // 얼빠지게 생긴 넘이다!! 죽여라!!
        await enemy.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        CameraManager.instance.SetCameraPosTarget(mainHero.element.cameraPos, false);

        // "앞에 황건적이네. 어쩌지?"
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);
        enemy.talkbox.SetActive(false);

        mainHero.move.MoveTarget(m_elementBase.enemy.First(), true);
        enemy.move.MoveTarget(mainHero, true);

        // "응? 적이 있으면 스스로 공격하는구나!!"
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        ControllerManager.instance.gameObject.SetActive(true);
        ControllerManager.instance.SetMove_HeroInfoDown(true, false);
        ControllerManager.instance.SetActive_Action(false);
        // "키보드 조작으로 내가 움직일 수 있을거 같은데?"
        mainHero.talkbox.Start(talk.Dequeue().talkArray);

        var prevPos = mainHero.transform.position;
        await UniTask.WaitUntil(() => (prevPos - mainHero.transform.position).sqrMagnitude > 2f, cancellationToken: token);

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

            await UniTask.NextFrame(cancellationToken: token);
        }

        enemy.target.SetTarget(null);
        enemy.move.MoveTarget(mainHero, true);
        mainHero.move.MoveTarget(enemy, true);

        ControllerManager.instance.SetMove_HeroInfoDown(false);

        var heroInfo = Scene_Lobby.instance.canvas.transform.Find("HeroInfo");
        Utils.SetActivePunch(heroInfo, true);

        var hi = heroInfo.GetComponentsInChildren<HeroInfoComponent>();
        for (int i = 0; i < hi.Length; i++)
            hi[i].StartStage();
        mainHero.buff.Remove(BuffType.DEBUFF_NO_SKILL);
        // 영웅 스킬 사용
        await mainHero.talkbox.StartAsync(talk.Dequeue().talkArray);
        await UniTask.WaitUntil(() => mainHero.attack.isUseSkill, cancellationToken: token);

        mainHero.buff.RemoveAll();
        enemy.buff.RemoveAll();

        mainHero.talkbox.Start(talk.Dequeue().talkArray);
        await UniTask.WaitUntil(() => enemy.isLive == false, cancellationToken: token);

        var rtArrow = (RectTransform)m_elementBase.arrows[0].transform;

        // 연회 열기
        if (DataManager.userInfo.myHero.Count == 1)
        {
            bottomButton[(int)LobbyScreenType.Summon].interactable = true;

            // 연회권 보상 연출
            await RewardWorker.instance.RunAsync(enemy.transform.position, ItemType.Normal_Gatcha_Ticket, _isField: true);

            await UniTask.WaitForSeconds(.5f, cancellationToken: token);

            // "연회권? 동료를 얻을 수 있으려나? 주막에 가보자."
            await mainHero.talkbox.StartAsync(talk.Dequeue().talkArray);

            await UniTask.WaitForSeconds(.5f, cancellationToken: token);

            rtArrow.gameObject.SetActive(true);
            // 영웅 뽑기
            await SummonHeroAsync(token);
            rtArrow.gameObject.SetActive(false);
        }
        else
            talk.Dequeue();

        // "영웅을 출전시켜보자."
        {
            await mainHero.talkbox.StartAsync(talk.Dequeue().talkArray);
            await UniTask.WaitForSeconds(.5f, cancellationToken: token);

            bottomButton[(int)LobbyScreenType.Summon].interactable = false;
            bottomButton[(int)LobbyScreenType.Hero].interactable = true;

            rtArrow.anchoredPosition += new Vector2(-800, 0);
            rtArrow.gameObject.SetActive(true);

            bool isRetry = false;
            while (true)
            {
                // 영웅 창 기다리기
                await UniTask.WaitUntil(() => LobbyScreenManager.instance.curScreen == LobbyScreenType.Hero, cancellationToken:token);

                // 꺼질 때가지 기다리기
                // 영웅 창 기다리기
                await UniTask.WaitUntil(() => LobbyScreenManager.instance.curScreen != LobbyScreenType.Hero, cancellationToken: token);

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

        ControllerManager.instance.SetSwitch(false);

        // 자아! 이제 출발이다!!
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        mainHero.anim.PlayAttack();
        await UniTask.WaitForSeconds(0.5f, cancellationToken: token);

        for (int i = 0; i < bottomButton.Count; i++)
            bottomButton[i].interactable = true;

        await FinishAsync(false);
    }

    public override async UniTask FinishAsync(bool _isSkip)
    {
        // 딤 켜주자
        await PopupManager.instance.ShowDimmAsync(true);

        if (_isSkip == true)
            TeamManager.instance.mainHero.talkbox.Cancel(true);

        StageManager.instance.ClearEnemyList();
        TutorialManager.instance.Complete(TutorialType.START);

        ControllerManager.instance.SetSwitch(true);
        TeamManager.instance.SetState(CharacterStateType.Wait);
        Signal.instance.ActiveHUD.Emit(true);

        BannerComponent.instance.SetActiveSkip(false);

        for (int i = 0; i < DataManager.userInfo.myHero.Count; i++)
        {
            var hero = DataManager.userInfo.myHero[i];
            hero.isBatch = true;
            DataManager.userInfo.Update(hero);
        }

        await TeamManager.instance.SpawnUpdateAsync();
        TeamManager.instance.RepositionToMain(0, true);

        //await PopupManager.instance.ShowDimmAsync(false);

        m_cts = m_cts.ReleaseCTS();
    }

    async UniTask SummonHeroAsync(CancellationToken _token)
    {
        var screen = LobbyScreenManager.instance.GetScreenSummon();

        while (screen == null)
        {
            screen = LobbyScreenManager.instance.GetScreenSummon();
            await UniTask.NextFrame(cancellationToken:_token);
        }

        screen.SetRegionType(TeamManager.instance.mainHero.info.regionType);

        bool isHeroSummon = false;

        // 영웅을 뽑고, 스크린을 닫을 떄까지 기다린다.
        while (true)
        {
            if (isHeroSummon == false && DataManager.userInfo.myHero.Count > 1)
            {
                m_elementBase.arrows[0].gameObject.SetActive(false);
                isHeroSummon = true;
            }

            // 영웅 소환을 했고, 리절트를 나왔으면 꺼주자
            if (DataManager.userInfo.myHero.Count > 1 && screen.isOpenResult == false)
            {
                Signal.instance.CloseLobbyScreen.Emit(LobbyScreenType.Summon);
                break;
            }

            await UniTask.NextFrame(cancellationToken: _token);
        }

        screen.SetRegionType(RegionType.NONE);
    }


    public async UniTask OnButtonAsync_Skip()
    {
        var result = await PopupManager.instance.OpenModalAsync("스킵??");

        if (result == StatusType.Success)
        {
            await Request_Summon(DataManager.userInfo.myHero[0].key);

            var canvasBottom = Scene_Lobby.instance.canvas.transform.Find("Bottom");
            canvasBottom.gameObject.SetActive(true);
            var panelBottom = canvasBottom.Find("Panel");

            for (int i = 0; i < panelBottom.childCount; i++)
            {
                panelBottom.GetChild(i).GetComponent<Button>().interactable = true;
            }

            await FinishAsync(true);
        }
        else
            BannerComponent.instance.SetActiveSkip(true);
    }

    async UniTask Request_Summon(string _hostKey)
    {
        List<TableItemData> result = new();

        #region 영웅 불러오기
        {
            await UniTask.WaitForEndOfFrame();
            List<TableHeroData> dbHeroes = TableManager.hero.list.Where(x => x.key.Equals(_hostKey) == false && x.isLock == false).ToList();

            int i = 0;

            if (TutorialManager.instance.IsComplete(TutorialType.START) == false)
            {
                i++;
                result.Add(new()
                {
                    key = ItemType.Dedicated_Soul_Stone,
                    value = _hostKey,
                    count = TableManager.hero.GetNeedSoul(GradeType.Normal),
                    category = ItemCategoryType.Soul_Stone,
                });

                var startHero = TableManager.region.Get(
                    TableManager.hero.Get(_hostKey).regionType).startHeroKey;

                for (; i < startHero.Length; i++)
                {
                    result.Add(new()
                    {
                        key = ItemType.Dedicated_Soul_Stone,
                        value = startHero[i],
                        count = 10,
                        category = ItemCategoryType.Soul_Stone,
                    });
                }
            }

            for (; i < 10; i++)
            {
                TableItemData itemData = TableManager.item.Get(UnityEngine.Random.value > 0.5f ? ItemType.Gold : ItemType.Rice);
                itemData.value = itemData.key.ToString();
                itemData.count = UnityEngine.Random.Range(1, 10) * 10;
                result.Add(itemData);
            }
        }
        #endregion 영웅 불러오기

        var keyHero = result.FindAll(x => x.key == ItemType.Dedicated_Soul_Stone).Select(x => x.value).ToArray();

        AddressableManager.instance.Load_HeroCharacterAsync(keyHero).Forget();
        await AddressableManager.instance.Load_HeroIconAsync(keyHero);
        SetItemDataAsync(result).Forget();
    }
    async UniTask SetItemDataAsync(List<TableItemData> _result)
    {
        long totalGold = 0, totalRice = 0;

        var keyItem = _result.FindAll(x => x.key != ItemType.Dedicated_Soul_Stone).Select(x => x.value).ToArray();
        await AddressableManager.instance.Load_ItemIconAsync(keyItem);

        Dictionary<string, long> resultSoul = new();
        for (int i = 0; i < _result.Count; i++)
        {
            var data = _result[i];

            if (data.key == ItemType.Gold)
                totalGold += data.count;
            else if (data.key == ItemType.Rice)
                totalRice += data.count;
            else if (data.key == ItemType.Dedicated_Soul_Stone)
            {
                if (resultSoul.ContainsKey(data.value))
                    resultSoul[data.value] += data.count;
                else
                {
                    data.isNew = DataManager.userInfo.GetHeroInfoData(data.value).isMine == false;
                    resultSoul.Add(data.value, data.count);
                }
            }
        }

        // SAVEDATA 재화 데이타 저장
        DataManager.userInfo.AddAsset(totalGold, totalRice, false, false);
        foreach (var soul in resultSoul)
            DataManager.userInfo.AddHeroSoul(soul.Key, (int)soul.Value);
    }
}
