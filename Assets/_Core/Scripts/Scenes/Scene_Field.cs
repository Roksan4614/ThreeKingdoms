using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;

public class Scene_Field : SceneBase
{
    protected override void Awake()
    {
    }

    private void Start()
    {
        LoadMainHeroAsync().Forget();
    }

    async UniTask LoadMainHeroAsync()
    {
        await AddressableManager.instance.InitializeAsync();
        await TableManager.instance.InitializeAsync();
        DataManager.option.Initialize();
        await TutorialManager.instance.InitializeAsync();
        await DataManager.instance.InitializeAsync();


        var team = transform.Find("Map/Heros/Team");
        for (int i = 0; i < team.childCount; i++)
            Destroy(team.GetChild(i).gameObject);

        var mainHeroData = DataManager.userInfo.myHero.Where(x => x.isMain).First();
        var obj = await AddressableManager.instance.GetHeroCharacterAsync(mainHeroData.key);

        var comp = Instantiate(obj, team).GetComponent<CharacterComponent>();
        comp.SetHeroData(mainHeroData.key);

        Signal.instance.ConnectMainHero.Emit(comp);
        ControllerManager.instance.isSwitch = true;
    }
}
