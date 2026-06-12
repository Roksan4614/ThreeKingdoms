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
        DataManager.option.Initialize();
        await UniTask.WaitForEndOfFrame();

        //await PopupManager.instance.ShowDimmAsync(false);
        //await PopupManager.instance.ShowDimmAsync(true);

        await TutorialManager.instance.InitializeAsync();
        await DataManager.instance.InitializeAsync();

        System.DateTime dtStart = System.DateTime.Now;
        List<UniTask> tasks = new();

        var keys = TableManager.hero.list.Select(x => x.key).ToArray();
        tasks.Add(AddressableManager.instance.Load_HeroIconAsync(keys));
        tasks.Add(AddressableManager.instance.Load_HeroCharacterAsync(keys));
        tasks.Add(AddressableManager.instance.Load_ItemIconAsync(TableManager.item.list.Select(x => x.key.ToString()).ToArray()));

        await UniTask.WhenAll(tasks);

        IngameLog.Add("LOAD ASSET: " + (System.DateTime.Now - dtStart).TotalSeconds);

        AddressableManager.instance.LoadScene("02_Lobby");
    }
}
