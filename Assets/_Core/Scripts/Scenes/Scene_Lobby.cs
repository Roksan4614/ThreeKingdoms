using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Scene_Lobby : SceneBase
{
    async void Start()
    {
        await UniTask.NextFrame();

        // 캐릭터가 없다면 선택 화면부터
        if (DataManager.userInfo.myHero.Count == 0)
            await PopupManager.instance.OpenPopupAndWait(PopupType.SelectRegion);

        await TeamManager.instance.SpawnUpdateAsync();

        //if (TutorialManager.instance.IsComplete(TutorialType.START) == false)
        //    await TutorialManager.instance.StartAsync(TutorialType.START);

        StageManager.instance.StartStageAsync().Forget();

        ControllerManager.instance.SetSwitch(true);

        StageManager.instance.TestDevSelectAsync(false).Forget();
        // 요일던전에서 나온거면
        if (DataManager.dailyDungeon.enterWeekday == WeekdayType.MAX)
        {
            DataManager.dailyDungeon.enterWeekday = WeekdayType.None;
            BottomComponent.instance.OnButton_OpenScreen(LobbyScreenType.Boss);
        }
        // 스토리에서 나온거면
        else if (DataManager.storyMode.isExit == true)
        {
            DataManager.storyMode.isExit = false;
            PopupManager.instance.OpenPopup(PopupType.LobbyStoryMode);
        }
        // 보스레이드
        else if (BossRaidWorker.instance.isExit)
        {
            BossRaidWorker.instance.isExit = false;
            PopupManager.instance.OpenPopup(PopupType.LobbyBossRaid);
        }
        else
        {
            //스토리 모드 처음 세개 순서 조정 해야 해.
            TableManager.storyNode.SortInitialize();

#if UNITY_EDITOR
            StageManager.instance.TestDevSelectAsync(true).Forget();
#endif
        }

        // TEST
        //TutorialManager.instance.Complete(TutorialType.CASTLE_FINISHED);
    }

    //private void Update()
    //{
    //    ScreenLogWorker.Add("DISANCE_MOUSE", (TeamManager.instance.mainHero.position - CameraManager.posPointer).sqrMagnitude);

    //}

    public override void OnManualValidate() { m_element.Initialize(transform); }

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public void Initialize(Transform _transform)
        {
        }
    }
}
