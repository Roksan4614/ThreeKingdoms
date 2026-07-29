using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        DataManager.option.Initialize();
        await UniTask.NextFrame();

        //await PopupManager.instance.ShowDimmAsync(false);
        //await PopupManager.instance.ShowDimmAsync(true);

        await TutorialManager.instance.InitializeAsync();

        System.DateTime dtStart = System.DateTime.Now;
        List<UniTask> tasks = new();

        tasks.Add(DataManager.instance.InitializeAsync());
        var keys = TableManager.hero.list.Select(x => x.key).ToArray();
        tasks.Add(AddressableManager.instance.Load_HeroIconAsync(keys));
        tasks.Add(AddressableManager.instance.Load_HeroCharacterAsync(keys));
        tasks.Add(AddressableManager.instance.Load_ItemIconAsync(TableManager.item.list.Select(x => x.key.ToString()).ToArray()));

        await UniTask.WhenAll(tasks);

        TimeManager.instance.InitializeAsync().Forget();

#if !UNITY_EDITOR
        IngameLog.Add($"Login: StartAsync: Finished: {(Time.realtimeSinceStartup - timeStart):0.#0}s");
#endif

        AddressableManager.instance.LoadScene("02_Lobby");
    }
}
