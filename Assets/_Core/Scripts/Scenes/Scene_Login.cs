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

        //await AddressableManager.instance.LoadAssetAsync<GameObject>(true, null, null, AddressableLabelType.L_Start);

        var keys = TableManager.hero.list.Select(x => x.key).ToArray();
        IngameLog.AddBuild("Load_HeroIconAsync");
        AddressableManager.instance.Load_HeroIconAsync(keys).Forget();
        IngameLog.AddBuild("Load_HeroCharacterAsync");
        AddressableManager.instance.Load_HeroCharacterAsync(keys).Forget();
        IngameLog.AddBuild("Load_ItemIconAsync");
        AddressableManager.instance.Load_ItemIconAsync(TableManager.item.list.Select(x => x.key.ToString()).ToArray()).Forget();
        IngameLog.AddBuild("Load_TreasureIconAsync");
        AddressableManager.instance.Load_ItemIconAsync(TableManager.treasure.list.Select(x => $"Treasure_{x.key}").ToArray()).Forget();
        IngameLog.AddBuild("Load_TierCionAsync");

        string[] tierKey = new string[8];
        for (int i = 0; i < tierKey.Length; i++)
            tierKey[i] = $"Tier_{i + 1}";
        AddressableManager.instance.Load_ItemIconAsync(tierKey).Forget();

        //AddressableManager.instance.Load_AllIcon();

        IngameLog.AddBuild("LOGIN START");

        // TODO: Login
        await TutorialManager.instance.InitializeAsync();
        await DataManager.instance.InitializeAsync();

        TimeManager.instance.InitializeAsync().Forget();

        var time = Time.realtimeSinceStartup - timeStart;
        if (time < 1)
            await UniTask.WaitForSeconds(1 - time);

        await PopupManager.instance.ShowDimmAsync(true);

#if !UNITY_EDITOR
        IngameLog.AddBuild($"Login: StartAsync: Finished: {(Time.realtimeSinceStartup - timeStart):0.#0}s");
#endif

        AddressableManager.instance.LoadScene("02_Lobby");
    }
}
