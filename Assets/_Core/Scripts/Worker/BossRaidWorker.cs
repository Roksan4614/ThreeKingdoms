using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using static Data_BossRaid;

public class BossRaidWorker : MonoSingleton<BossRaidWorker>
{
    public enum BossRaidType
    {
        NONE = -1,

        LuBu,   // 여포
    }

    bool m_isDoing = false;
    AsyncOperationHandle<GameObject> m_handle;

    BossRaidType m_bossType = BossRaidType.NONE;
    public BossRaidType bossType => m_bossType;
    public bool isRunning => m_bossType > BossRaidType.NONE;

    public async UniTask InitializeAsync(BossRaidType _bossType)
    {
        if (m_isDoing == false)
            m_isDoing = true;

        await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);

        PopupManager.instance.CloseAll();

        TeamManager.instance.SetState(CharacterStateType.None);
        StageManager.instance.SetState(CharacterStateType.None);

        await UniTask.WaitForEndOfFrame();

        AddressableManager.instance.LoadScene("03_BossRaid");
        m_bossType = _bossType;

        //bool isStart = await StartAsync(_bossType);
        //await PopupManager.instance.ShowDimmAsync(false, _duration: isStart ? 1f : .5f);

        m_isDoing = false;
    }

    async UniTask<bool> ConnectAsync()
    {
        await UniTask.Yield();

        return true;
    }

    async UniTask OpenResultAsync()
    {
        await UniTask.Yield();
    }

    protected override void OnDestroy()
    {
        if (m_handle.IsValid())
            m_handle.Release();

        base.OnDestroy();
    }

    public void StartBossRaid()
    {
        DataManager.bossRaid.Start_BossRaid();

        TeamManager.instance.StartStage();
        TeamManager.instance.StartPhase(false);

        StageManager.instance.SetState(CharacterStateType.Battle);

        ControllerManager.instance.SetSwitch(true);
        ControllerManager.instance.SlotStartStage();

        InfoStageComponent.instance.SetBossRaid(true);

        TeamManager.instance.SetLockSkill(false);
    }

    public void Finish_Phase(CharacterComponent _boss)
    {
        TeamManager.instance.SetLockSkill(true);

        float fTimeScale = 0.05f; // 느려지는 타임스케일
        float fMoveX = 3f;        // 뒤로 밀쳐지는 거리
        float fSlowDuration = 0.1f;   // 느려지는 거 유지 시간

        Time.timeScale = fTimeScale;

        // 보스 이펙트 초기화
        _boss.attack.ResetFX();

        // 카메라 흔들리면서 보스 따라가기
        CameraManager.instance.Shake();
        CameraManager.instance.SetCameraPosTarget(_boss.element.cameraPos, false);

        // 시간 다시 원래대로
        Utils.AfterSecond(() => Time.timeScale = 1f, fSlowDuration);

        // 뒤로 밀치기
        var targetPos = _boss.transform.position;
        targetPos.x = _boss.transform.position.x + (fMoveX * (_boss.move.isFlip ? -1 : 1));
        DOTween.To(() => _boss.transform.position, _pos => _boss.rig.MovePosition(_pos), targetPos, 0.3f)
            .OnComplete(() =>
            {
                if (DataManager.bossRaid.raidStatus == BossRaidStatusType.FirstPhase)
                    DataManager.bossRaid.Finish_FirstPhase();
                else
                
                    DataManager.bossRaid.Finish_BossRaid();
                
            });
    }

    public void Wait_SecondPhase()
    {
        Signal.instance.BossRaidStatus.Emit(BossRaidStatusType.Wait_SecondPhase);
    }

    public void Start_SecondPhase()
    {
        TeamManager.instance.SetLockSkill(false);
        CameraManager.instance.SetCameraPosTarget(TeamManager.instance.mainHero.element.cameraPos, false);
        DataManager.bossRaid.Start_SecondPhase();
    }

    public async UniTask ExitAsync()
    {
        m_bossType = BossRaidType.NONE;
        DataManager.bossRaid.ExitBossRaid();

        await PopupManager.instance.ShowDimmAsync(true, _duration: 0.2f);
        PopupManager.instance.CloseAll();
        await UniTask.WaitForEndOfFrame();
        AddressableManager.instance.LoadScene("02_Lobby");
    }
}
