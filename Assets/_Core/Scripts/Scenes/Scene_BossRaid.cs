using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class Scene_BossRaid : SceneBase
{
    bool m_isExit;
    private void Start()
    {
        StartAsync().Forget();

        Signal.instance.ActiveHUD.connectLambda = new(this, _isActive => { });
    }

    async UniTask StartAsync()
    {
        List<UniTask> tasks = new();
        tasks.Add(TeamManager.instance.SpawnUpdateAsync());
        tasks.Add(StageManager.instance.InitializeAsync_BossRaid());

        await UniTask.WhenAll(tasks);

        TeamManager.instance.StartStage();
        TeamManager.instance.StartPhase(false);

        StageManager.instance.SetState(CharacterStateType.Battle);

        ControllerManager.instance.SetSwitch(true);
        ControllerManager.instance.SlotStartStage();

        InfoStageComponent.instance.SetBossRaid(true);

        PopupManager.instance.ShowDimm(false);

    }

    private void Update()
    {
        if (m_isExit == false && Input.GetKeyDown(KeyCode.Escape))
        {
            m_isExit = true;
            BossRaidWorker.instance.FinishedAsync().Forget();
        }
    }

    public override void OnManualValidate() { m_elementBase.Initialize(transform); }

    struct ElementData
    {
    }

}
