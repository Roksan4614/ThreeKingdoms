using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ThreeKingdoms.Client.Server;
using UnityEngine;

public partial class GuideQuestComponent
{
    CancellationTokenSource m_serverGuideRun;
    string m_serverRunIssue;

    void StartServerGuide()
    {
        var manager = TutorialManager.instance;
        m_element.textTitle.text = manager.ServerTitle;
        m_element.textStatus.text = manager.ServerStatus;
        m_element.textStatus.gameObject.SetActive(true);
        m_element.complete.SetActive(manager.ServerProgressComplete);
        m_element.button.interactable = manager.HasServerIssue && manager.ServerContentReady && !manager.ServerClaiming;
        m_element.reward.gameObject.SetActive(manager.HasServerIssue);
        if (manager.HasServerIssue)
            m_element.reward.SetItemData(ServerState.ToItem(manager.ServerIssue.RewardItemId, manager.ServerIssue.RewardAmount));
        if (manager.ServerProgressComplete) HostOutAsync().Forget();
        BeginServerGuideRun();
    }

    void BeginServerGuideRun()
    {
        var manager = TutorialManager.instance;
        // A successful claim publishes the next issue before its finally block clears ServerClaiming.
        // Starting now could consume a one-time true condition while progress recording is still blocked.
        if (manager.ServerClaiming) return;
        var issue = manager.ServerIssue;
        if (issue?.IssueId == m_serverRunIssue) return;
        m_serverGuideRun?.Cancel(); m_serverGuideRun?.Dispose(); m_serverGuideRun = null;
        m_serverRunIssue = issue?.IssueId;
        if (!manager.ServerContentReady || manager.ServerProgressComplete) return;
        m_serverGuideRun = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        RunServerGuideAsync(issue.IssueId, issue.QuestKey, m_serverGuideRun.Token)
            .Forget(error => { if (!(error is OperationCanceledException)) GameServer.Report(error); });
    }

    async UniTask RunServerGuideAsync(string issueId, string key, CancellationToken token)
    {
        await UniTask.WaitUntil(() => TeamManager.instance?.mainHero != null, cancellationToken: token);
        var manager = TutorialManager.instance;
        var first = TeamManager.instance.mainHero;
        var origin = first.position;
        var previousMain = first.info.key;
        var wasActive = false;
        var attackCounts = new Dictionary<Character_Worker_Attack, long>();
        var skillCounts = new Dictionary<Character_Worker_Attack, long>();
        foreach (var hero in TeamManager.instance.members.Values)
        {
            attackCounts[hero.attack] = hero.attack.controlAttackCount;
            skillCounts[hero.attack] = hero.attack.skillUseCount;
        }
        attackCounts[first.attack] = first.attack.controlAttackCount;
        skillCounts[first.attack] = first.attack.skillUseCount;
        var arrow = key == "normal_attack" ? GuideQuestType.normal_attack : key == "main_skill_use" ? GuideQuestType.main_skill_use
            : key == "dash_use" ? GuideQuestType.dash_use : GuideQuestType.NONE;
        if (arrow != GuideQuestType.NONE) ControllerManager.instance.SetActive_GuideQuestArrow(true, arrow);
        try
        {
            while (manager.ServerIssue?.IssueId == issueId && !manager.ServerProgressComplete)
            {
                token.ThrowIfCancellationRequested();
                var main = TeamManager.instance.mainHero;
                if (main != null)
                {
                    var active = false;
                    long actionCount = 0;
                    switch (key)
                    {
                        case "move": active = ControllerManager.instance.isDoing && (main.position - origin).sqrMagnitude > 4; break;
                        case "normal_attack": actionCount = ObserveActions(attackCounts, main.attack, main.attack.controlAttackCount); break;
                        case "main_skill_use": actionCount = ObserveActions(skillCounts, main.attack, main.attack.skillUseCount); break;
                        case "dash_use": active = main.move.isDash; break;
                        case "character_deploy": active = TeamManager.instance.members.Count > 1; break;
                        case "sub_skill_use":
                            foreach (var hero in TeamManager.instance.members.Values)
                                if (hero != main) actionCount += ObserveActions(skillCounts, hero.attack, hero.attack.skillUseCount);
                            break;
                        case "auto_play_active": active = DataManager.option.isAutoSkill; break;
                        case "change_main": active = main.info.key != previousMain; break;
                        case "castle": active = LobbyScreenManager.instance.curScreen == LobbyScreenType.Castle && DataManager.castle.ServerCastle != null; break;
                    }
                    for (long index = 0; index < actionCount && !manager.ServerProgressComplete; index++)
                        manager.ServerRecordProgress(key, issueId: issueId);
                    if (active && !wasActive) manager.ServerRecordProgress(key, issueId: issueId);
                    wasActive = active;
                }
                await UniTask.NextFrame(token);
            }
        }
        finally
        {
            if (arrow != GuideQuestType.NONE && ControllerManager.instance != null)
                ControllerManager.instance.SetActive_GuideQuestArrow(false, arrow);
        }
    }

    static long ObserveActions(Dictionary<Character_Worker_Attack, long> observed, Character_Worker_Attack worker, long current)
    {
        // Newly spawned workers start their lifetime counters at zero.
        observed.TryGetValue(worker, out var previous);
        observed[worker] = current;
        return Math.Max(0, current - previous);
    }

    void OnServerGuideButton()
    {
        var manager = TutorialManager.instance;
        if (!manager.HasServerIssue || manager.ServerClaiming || !manager.ServerContentReady) return;
        if (manager.ServerProgressComplete) { RewardServerGuideAsync().Forget(GameServer.Report); return; }
        var navigation = manager.ServerIssue.Navigation.Split(',')[0].Trim().ToLowerInvariant();
        switch (navigation)
        {
            case "character": LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Hero); break;
            case "castle": LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Castle); break;
            case "dungeon": LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Boss); break;
            case "gacha": LobbyScreenManager.instance.OpenScreen(LobbyScreenType.Summon); break;
            case "raid": PopupManager.instance.OpenPopup(PopupType.LobbyBossRaid); break;
            case "tournament": PopupManager.instance.OpenPopup(PopupType.LobbyTournament); break;
            default: HostTalkboxStart(manager.ServerTitle); break;
        }
    }

    async UniTask RewardServerGuideAsync()
    {
        var result = await TutorialManager.instance.ServerClaimAsync();
        if (result == null || !this) return;
        var display = ServerState.ToItem(result.RewardItemId, result.RewardAmount);
        // This overload only animates; ServerClaimAsync already applied the authoritative absolute balances.
        await RewardWorker.instance.RunAsync(m_element.reward.transform.position, display.key, display.count, _isPopup: true);
    }

    protected override void OnDestroy()
    {
        TutorialManager.instance.ServerIssueChanged -= StartServerGuide;
        m_serverGuideRun?.Cancel(); m_serverGuideRun?.Dispose();
        base.OnDestroy();
    }
}
