using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rev9.Tournament
{
    public enum TournamentStatusType
    {
        Wait,
        Start,
        Finished
    }

    public partial class TournamentWorker
    {
        public TournamentStatusType statusType { get; private set; }

        public TournamentRankerUserData enterUserData { get; private set; }
        public bool isRunning => enterUserData.IsActive();
        int m_idxRevenge;

        public async UniTask EnterBattleAsync(int _uid, int _idxRevenge = -1)
        {
            if (m_data.countPlay == 0)
                return;

            if (ThreeKingdoms.Client.Server.GameServer.Enabled && !await EnterServerBattleAsync(_uid, _idxRevenge)) return;

            await PopupManager.instance.ShowDimmAsync(true);

            m_idxRevenge = _idxRevenge;
            statusType = TournamentStatusType.Wait;

            StageManager.instance.SetState(CharacterStateType.None);
            TeamManager.instance.SetState(CharacterStateType.None);

            PopupManager.instance.CloseAll();

            IngameLog.Add("Enter: " + _uid);
            if (!ThreeKingdoms.Client.Server.GameServer.Enabled) enterUserData = m_data.GetUserData(_uid);

            await UniTask.WaitUntil(() => PopupManager.instance.IsOpenPopup() == false);

            AddressableManager.instance.LoadScene("03_Tournament");
        }

        public void Finished()
        {
            if (ThreeKingdoms.Client.Server.GameServer.Enabled) { FinishAndShowServerAsync().Forget(); return; }
            if (statusType == TournamentStatusType.Finished)
                return;

            statusType = TournamentStatusType.Finished;

            SaveHistoryAsync().Forget();
            PopupManager.instance.CloseAll();
            PopupManager.instance.OpenPopup(PopupType.TournamentResult);
            Signal.instance.TournamentStatus.Emit(TournamentStatusType.Finished);
        }

        public async UniTask SaveHistoryAsync()
        {
            if (ThreeKingdoms.Client.Server.GameServer.Enabled) { await FinishServerBattleAsync(); return; }
            bool isWin = TournamentHeroInfoManager.instance.IsWin();

            await API_AddHistoryData(true, isWin, enterUserData, m_idxRevenge);

            m_data.countPlay--;

            await API_LoadBattleListAsync();
            SaveData();
        }

        public async UniTask ExitAsync()
        {
            await AddressableManager.instance.LoadSceneAsync("02_Lobby");

            var tourament = await PopupManager.instance.OpenPopupAsync<PopupTournamentComponent>(PopupType.LobbyTournament);
            if (m_idxRevenge > -1)
                tourament.OpenPopupAsync(PopupTournamentComponent.TournamentPopupType.History).Forget();

            enterUserData = default;
            PopupManager.instance.ShowDimm(false);
        }

        public int GetResultPoint(bool _isWin)
        {
            if (ThreeKingdoms.Client.Server.GameServer.Enabled) return checked((int)(m_serverResult?.AttackerScoreDelta ?? (_isWin ? m_serverEntry?.WinScore : m_serverEntry?.LoseScore) ?? 0));
            int point = 0;
            for (int i = 0; i < m_data.battleUserList.Length; i++)
            {
                if (enterUserData.info.uid == m_data.battleUserList[i].info.uid)
                    point = i == 0 ? 30 : i == 1 ? 20 : i == 2 ? 15 : 5;
            }

            if (_isWin == false)
                point = point - 35;

            return point;
        }

        public int GetResultRewardCount(bool _isWin)
        {
            if (ThreeKingdoms.Client.Server.GameServer.Enabled) return checked((int)(m_serverResult?.PointDelta ?? (_isWin ? m_serverEntry?.PointWin : m_serverEntry?.PointLose) ?? 0));
            return _isWin ? 100 : 30;
        }

    }
}