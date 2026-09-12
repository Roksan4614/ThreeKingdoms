using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ThreeKingdoms.Client.Server;
using ThreeKingdoms.Shared.Types;

public partial class Data_UserInfo
{
    public async UniTask ServerLoginAsync()
    {
        ServerState.Reset();
        var login = await GameServer.LoginAsync();
        m_element.Default();
        ApplyServerUser(login.UserInfo);
        m_sortData = PPWorker.HasKey(PlayerPrefsType.HERO_DATA_SORTING, false)
            ? PPWorker.Get<HeroSortData>(PlayerPrefsType.HERO_DATA_SORTING, false) : new HeroSortData();
        if (m_sortData.filter_class == null) m_sortData.Default();
        idleRewardData = new IdleRewardData();
        idleRewardData.Default();
        ServerState.ApplyAsset(login.Asset);
        if (login.RegionSelected) await ServerState.ApplyCharactersAsync((await GameServer.Character.LobbyAsync(new EmptyRes(), GameServer.Options())).Data);
    }
    public void ApplyServerUser(UserInfoDto user)
    {
        m_element.userInfoData.uid = checked((int)user.Uid);
        m_element.userInfoData.nickname = user.Nickname;
        m_element.userInfoData.region = user.Region.HasValue ? (RegionType)(int)user.Region.Value : RegionType.NONE;
        // ProfileIconCompoent uses 0 for a hero icon selected by its skin key.
        // Server character IDs are table IDs, not the client's numeric profile slot.
        m_element.userInfoData.profileIdx = 0;
        m_element.userInfoData.profileSkin = user.ProfileCharacterId.HasValue
            ? (string)GameServer.TableRows("s_character").Single(row => (long)row["idx"] == user.ProfileCharacterId.Value)["key"]
            : null;
        m_element.userInfoData.batchHeroes ??= new List<HeroInfoData>();
        m_element.userInfoData.treasures ??= new List<string>();
        if (GameServer.Login != null) { GameServer.Login.UserInfo = user; GameServer.Login.RegionSelected = user.Region.HasValue; }
    }
    public void ApplyServerCharacters(List<HeroInfoData> heroes, IReadOnlyList<TreasureStateDto> treasures)
    {
        m_element.myHero = heroes;
        m_element.userInfoData.treasures = treasures.Where(item => item.EquippedSlot.HasValue).OrderBy(item => item.EquippedSlot).Select(item => item.TreasureKey).ToList();
        m_element.userInfoData.batchHeroes = heroes.Where(hero => hero.isBatch).ToList();
        SaveData();
    }
    public async UniTask ServerSelectRegionAsync(RegionType region)
    {
        var result = (await GameServer.User.SelectRegionAsync(new SelectRegionReq { Region = (ThreeKingdoms.Shared.Enums.RegionType)(int)region }, GameServer.Options())).Data;
        ApplyServerUser(result.UserInfo);
        await ServerState.ApplyCharactersAsync(result.CharacterSnapshot);
        await DataManager.instance.InitializeAsync();
        await TutorialManager.instance.ServerRefreshAsync();
    }
}
