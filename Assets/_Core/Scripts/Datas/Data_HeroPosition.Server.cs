using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ThreeKingdoms.Client.Server;
using ThreeKingdoms.Shared.Enums;
using ThreeKingdoms.Shared.Types;

public partial class Data_HeroPosition
{
    private readonly Dictionary<HeroPositionType, CharacterPositionDto> m_serverPositions = new();
    private bool m_serverBinding;
    public CharacterPositionDto ServerPosition(HeroPositionType type)
        => m_serverPositions.TryGetValue(type, out var position) ? position : null;
    public string ServerStatusLabel(HeroPositionType type)
    {
        var position = ServerPosition(type);
        if (position == null || position.Status == CharacterPositionStatus.Unavailable) return " (준비 중)";
        return position.Status == CharacterPositionStatus.Locked ? $" (잠김 {position.UnlockCurrent ?? "-"}/{position.UnlockRequired})" : "";
    }

    public async UniTask RefreshServerAsync()
    {
        if (!GameServer.Enabled) return;
        var response = await GameServer.Character.PositionSyncAsync(new EmptyRes(), GameServer.Options());
        ApplyServerPositions(response.Data?.Positions ?? throw new InvalidOperationException("Character positions response is empty."));
    }
    private void ApplyServerPositions(IReadOnlyList<CharacterPositionDto> positions)
    {
        data = new List<HeroPositionData>();
        m_serverPositions.Clear();
        foreach (var position in positions)
        {
            var type = CharacterPositionCatalog.FromServerKey(position.PositionKey);
            m_serverPositions[type] = position;
            if (!position.CharacterId.HasValue) continue;
            var hero = ServerState.Characters?.Characters.FirstOrDefault(x => x.CharacterId == position.CharacterId.Value);
            if (hero != null) data.Add(new HeroPositionData { type = type, heroKey = hero.CharacterKey });
        }
    }
    private async UniTask<bool> BindServerAsync(string heroKey, HeroPositionType type)
    {
        if (m_serverBinding) return false;
        m_serverBinding = true;
        try
        {
            await RefreshServerAsync();
            var position = ServerPosition(type) ?? throw new InvalidOperationException("직책 정보를 다시 불러와 주세요.");
            var heroId = ServerState.CharacterId(heroKey);
            var clear = position.CharacterId == heroId;
            if (!clear && position.Status != CharacterPositionStatus.Unlocked)
                throw new InvalidOperationException(position.Status == CharacterPositionStatus.Unavailable ? "아직 이용할 수 없는 직책입니다." : "직책 해금 조건을 충족하지 못했습니다.");
            if (!clear && !position.EligibleCharacterIds.Contains(heroId))
                throw new InvalidOperationException("이 무장은 직책의 장착 조건을 충족하지 못했습니다.");
            if (!clear && position.CharacterId.HasValue && await PopupManager.instance.OpenModalAsync("기존에 배치된 무장을 교체하시겠습니까?") != StatusType.Success)
                return false;
            var result = await CharacterActions.SetPositionAsync(position.PositionId, clear ? null : heroId);
            ApplyServerPositions(result.Positions);
            return true;
        }
        catch (Exception error) { PopupManager.instance.AlertShow(error.Message); return false; }
        finally { m_serverBinding = false; }
    }
}
