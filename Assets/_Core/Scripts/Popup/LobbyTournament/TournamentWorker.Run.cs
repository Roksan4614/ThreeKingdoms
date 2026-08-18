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
        public TournamentStatusType statusType;

        public TournamentRankerUserData enterUserData { get; private set; }
        public bool isRunning => enterUserData.isActive;

        public async UniTask EnterBattleAsync(int _uid, int _idxRevenge = -1)
        {
            if (m_data.countPlay == 0)
                return;

            await PopupManager.instance.ShowDimmAsync(true);

            statusType = TournamentStatusType.Wait;

            StageManager.instance.SetState(CharacterStateType.None);
            TeamManager.instance.SetState(CharacterStateType.None);

            PopupManager.instance.CloseAll();

            IngameLog.Add("Enter: " + _uid);
            enterUserData = m_data.GetUserData(_uid);

            await UniTask.WaitUntil(() => PopupManager.instance.IsOpenPopup() == false);

            AddressableManager.instance.LoadScene("03_Tournament");
        }

        public async UniTask ExitAsync(int _uid, int _idxRevenge = -1)
        {
            var historyData = m_history.Find(x => x.uid == _uid);

            if (historyData.isActive == true)
            {
                bool isWin = historyData.batchData.totalPower < m_data.teamAttack.totalPower * 1.2f;
                await API_AddHistoryData(true, isWin, new()
                {
                    batchData = historyData.batchData,
                    info = new()
                    {
                        uid = historyData.uid,
                        indexProfile = historyData.indexProfile,
                        skin = historyData.skin,
                        nickname = historyData.nickname,
                        power = historyData.batchData.totalPower,
                    }
                }, _idxRevenge);
            }
            else
            {
                foreach (var user in m_data.battleUserList)
                {
                    if (user.info.uid == _uid)
                    {
                        bool isWin = user.batchData.totalPower < m_data.teamAttack.totalPower * 1.2f;
                        await API_AddHistoryData(true, isWin, user, _idxRevenge);
                        break;
                    }
                }
            }

            enterUserData = default;
            m_data.countPlay--;
            SaveData();

            await API_LoadBattleListAsync();

            var tourament = await PopupManager.instance.OpenPopupAsync<PopupTournamentComponent>(PopupType.LobbyTournament);
            if (_idxRevenge > -1)
                tourament.OpenPopupAsync(PopupTournamentComponent.TournamentPopupType.History).Forget();

            PopupManager.instance.ShowDimm(false);
        }

    }
}