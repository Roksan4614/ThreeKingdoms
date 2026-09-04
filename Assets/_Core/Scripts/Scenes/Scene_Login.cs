using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Scene_Login : SceneBase
{
    private void Start()
    {
        PopupManager.instance.ShowDimm(true, false);
        PopupManager.instance.SetCanvasCamera();

        StartAsync().Forget();
    }

    async UniTask StartAsync()
    {
        var timeStart = Time.realtimeSinceStartup;

        var circle = transform.Find("Canvas/Circle");

        circle.transform.DORotate(new Vector3(0f, 0f, 360f), 20f, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear).Forget();

        DataManager.option.Initialize();
        PopupManager.instance.ShowDimm(false);
        await UniTask.NextFrame();

        System.DateTime dtStart = System.DateTime.Now;

        List<AddressableLabelType> labelIcon = new() { };

        long totalSize = await AddressableManager.instance.GetDownloadSizeAsync(true, AddressableLabelType.L_Start);

        IngameLog.AddBuild("TOTAL SIZE: START LABEL: " + Utils.FileSize(totalSize));

        List<UniTask> tasks = new();
        var keys = TableManager.hero.list.Select(x => x.key).ToArray();
        IngameLog.AddBuild("Load_HeroIconAsync");
        tasks.Add(AddressableManager.instance.Load_HeroIconAsync(keys));
        IngameLog.AddBuild("Load_HeroCharacterAsync");
        tasks.Add(AddressableManager.instance.Load_HeroCharacterAsync(keys));
        IngameLog.AddBuild("Load_ItemIconAsync");
        tasks.Add(AddressableManager.instance.Load_ItemIconAsync(TableManager.item.list.Select(x => x.key.ToString()).ToArray()));
        IngameLog.AddBuild("Load_TreasureIconAsync");
        tasks.Add(AddressableManager.instance.Load_ItemIconAsync(TableManager.treasure.list.Select(x => $"Treasure_{x.key}").ToArray()));
        IngameLog.AddBuild("Load_TierCionAsync");

        string[] tierKey = new string[8];
        for (int i = 0; i < tierKey.Length; i++)
            tierKey[i] = $"Tier_{i + 1}";
        tasks.Add(AddressableManager.instance.Load_ItemIconAsync(tierKey));

        IngameLog.AddBuild("LOGIN START");

        // TODO: Login
        await TutorialManager.instance.InitializeAsync();
        await DataManager.instance.InitializeAsync();

        TimeManager.instance.InitializeAsync().Forget();

        if (DataManager.userInfo.myHero.Count == 0)
            tasks.Add(PopupManager.instance.LoadAsset(PopupType.SelectRegion));

        tasks.Add(AddressableManager.instance.DownloadAsync(true, null, "02_Lobby"));
        tasks.Add(LoadLobbyScreenAsync());

        await UniTask.WhenAll(tasks.ToArray());
        IngameLog.AddBuild($"Login: StartAsync: Finished: {(Time.realtimeSinceStartup - timeStart):0.#0}s");

        var time = Time.realtimeSinceStartup - timeStart;
        if (time < 1)
            await UniTask.WaitForSeconds(1 - time);

        IngameLog.Add("Load Scene: Lobby");

        await PopupManager.instance.ShowDimmAsync(true);

        AddressableManager.instance.LoadScene("02_Lobby");

        IngameLog.Add("Login: Finished");
    }

    public async UniTask LoadLobbyScreenAsync()
    {
        IngameLog.AddBuild("LoadLobbyScreenAsync: Start");
        var instantiateScreen = new List<LobbyScreenType>() { LobbyScreenType.Hero, LobbyScreenType.Castle };

        for (int i = 0; i < instantiateScreen.Count; i++)
            await AddressableManager.instance.Load_LobbyScreenAsync(instantiateScreen[i]);

        IngameLog.AddBuild("LoadLobbyScreenAsync: Finished");
    }
}
