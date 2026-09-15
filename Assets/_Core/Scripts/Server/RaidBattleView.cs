using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using ThreeKingdoms.Client.Server;
using UnityEngine;

public partial class Character_Enemy_RaidBoss
{
    private void SetServerBossData(string key)
    {
        var round = DataManager.bossRaid.ServerRound ?? throw new InvalidOperationException("RAID_ROUND_REQUIRED");
        isBoss = true;
        m_stat = TableManager.statHero.GetStatData(key ?? round.BossKey) ?? throw new InvalidOperationException("RAID_BOSS_STATS_MISSING");
        ApplyServerBossStats();
        SetFaction(FactionType.Enemy);
        SetActive_HP(false);
    }

    public void ApplyServerBossStats()
    {
        var round = DataManager.bossRaid.ServerRound;
        if (round == null || m_stat == null) return;
        m_stat.healthMax = long.Parse(round.MaxHp);
        m_stat.health = long.Parse(round.Hp);
        m_stat.attackPower = (float)round.AttackPower;
        m_stat.defenceValue = (float)round.Defence;
    }

    private bool ServerDamage(CharacterComponent attacker, float damage, bool critical)
    {
        var phase = DataManager.bossRaid.ServerRound?.Phase;
        if ((phase != "normal" && phase != "jin") || buff.IsActive(BuffType.BUFF_NO_TAKEN_DAMAGE) || damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage)) return false;
        if (attacker != null && attacker.factionType != FactionType.Alliance) return false;
        var amount = damage >= long.MaxValue ? long.MaxValue : (long)damage;
        if (amount <= 0) return false;
        if (attacker != null)
            EffectWorker.instance.SlotDamageTakenEffect(new()
            { attacker = attacker.transform, target = this, value = -amount, isCritical = critical, isAlliance = false });
        DataManager.bossRaid.QueueServerDamage(amount);
        return false;
    }
}

public partial class BossRaidWorker
{
    private void StartServerBattleView()
    {
        TeamManager.instance.StartStage();
        TeamManager.instance.StartPhase(false);
        ControllerManager.instance.SetSwitch(true);
        ControllerManager.instance.SlotStartStage();
        InfoStageComponent.instance.SetBossRaid(true);
        ArrowNaviComponent.instance.SetTarget(TeamManager.instance.mainHero.transform);
        UnityEngine.Object.FindFirstObjectByType<BossRaid_BossSlotComponent>()?.BeginServerBattle();
        DataManager.bossRaid.EmitServerPhase();
    }
    public void ApplyServerResult(bool success) => isSuccessed = success;
}

public partial class BossRaid_BossSlotComponent
{
    private string serverPhase;
    private bool serverResultShown;
    private bool serverBattleReady;
    private void InitializeServerBossView()
    {
        DataManager.bossRaid.ServerRaidChanged += ApplyServerBossView;
        Signal.instance.BossRaidStatus.connect = ServerPhaseChanged;
        ApplyServerBossView();
    }
    private void ServerPhaseChanged(Data_BossRaid.BossRaidStatusType _) => ApplyServerBossView();
    public void BeginServerBattle() { serverBattleReady = true; serverPhase = null; ApplyServerBossView(); }

    private void ApplyServerBossView()
    {
        var round = DataManager.bossRaid.ServerRound;
        if (round == null || !gameObject.activeInHierarchy) return;
        var jin = DataManager.bossRaid.data.tickSecondPhase > 0;
        m_element.boss.gameObject.SetActive(!jin);
        m_element.bossJIN.gameObject.SetActive(jin);
        var active = jin ? m_element.bossJIN : m_element.boss;
        if (!serverBattleReady) { active.SetBossData(round.BossKey); return; }
        if (serverPhase != round.Phase)
        {
            if (round.Phase == "jin")
            {
                m_element.bossJIN.position = m_element.boss.position;
                m_element.fxChangeBoss.SetActive(false);
            }
            active.SetBossData(round.BossKey);
            StageManager.instance.ClearEnemyList();
            StageManager.instance.AddEnemyList(active);
            if (round.Phase == "normal" || round.Phase == "jin")
            {
                TeamManager.instance.RemoveBuff(BuffType.BUFF_NO_TAKEN_DAMAGE);
                active.buff.Remove(BuffType.BUFF_NO_TAKEN_DAMAGE);
                TeamManager.instance.SetState(CharacterStateType.SearchEnemy);
                StageManager.instance.SetState(CharacterStateType.Battle);
                (Scene_BossRaid.instance as Scene_BossRaid)?.SetActiveResult(false, false);
                InfoStageComponent.instance.SetActive(true, true);
            }
            else
            {
                TeamManager.instance.AddBuff(BuffType.BUFF_NO_TAKEN_DAMAGE);
                TeamManager.instance.SetState(CharacterStateType.None);
                StageManager.instance.SetState(CharacterStateType.None);
                if (round.Phase == "transition")
                {
                    m_element.fxChangeBoss.transform.position = active.position;
                    m_element.fxChangeBoss.SetActive(true);
                    active.anim.Play(CharacterAnimType.Die_1);
                }
                if (round.Phase == "finished" && !serverResultShown)
                {
                    serverResultShown = true;
                    if (long.Parse(round.Hp) == 0) active.anim.Play(CharacterAnimType.Die_1);
                    BossRaidWorker.instance.ApplyServerResult(long.Parse(round.Hp) == 0);
                    PopupManager.instance.OpenPopup(PopupType.BossRaidResult);
                }
            }
            serverPhase = round.Phase;
        }
        active.ApplyServerBossStats();
    }
}

public partial class InfoStage_Boss
{
    private void ApplyServerRaidHud()
    {
        if (m_elementBossRiad.txtTimer == null) return;
        m_element.txtName.text = DataManager.bossRaid.data.bossName;
        ServerRaidTimerAsync().Forget();
    }
    private async UniTask ServerRaidTimerAsync()
    {
        m_ctsTimer = m_ctsTimer.ReleaseCTS(true);
        var token = m_ctsTimer.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var round = DataManager.bossRaid.ServerRound;
                rtTimer.gameObject.SetActive(round != null && round.Phase != "finished");
                m_elementBossRiad.txtTimer.text = round?.Phase == "transition" ? "진 레이드 전환 중" : TimeSpan.FromSeconds(DataManager.bossRaid.ServerRemainingSeconds).ToRemainTime(15, _isStartMinute: true);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException) { }
    }
}

public partial class Weapon_Vanguard_Lubu_BossRaid
{
    private async UniTask ServerSkillAsync()
    {
        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;
        var lastPattern = -1;
        string phase = null;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var round = DataManager.bossRaid.ServerRound;
                var activePhase = m_isJIN ? "jin" : "normal";
                if (round?.Phase == activePhase && m_dbSkills.Count > 0)
                {
                    if (phase != round.Phase)
                    {
                        phase = round.Phase;
                        var elapsed = DataManager.bossRaid.ServerNow.Ticks - Data_Castle.ServerTicks(round.PatternStartedAt);
                        // A reconnect joins the next shared pattern edge instead of replaying an old attack late.
                        lastPattern = elapsed > TimeSpan.FromMilliseconds(500).Ticks ? round.PatternIndex : -1;
                    }
                    if (round.PatternIndex != lastPattern && TeamManager.instance.GetRandomHero(true) != null)
                    {
                        lastPattern = round.PatternIndex;
                        m_skillType = BossRaidSkillType_LuBu.NONE;
                        await m_dbSkills[lastPattern % m_dbSkills.Count].async();
                    }
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException) { }
    }
}
