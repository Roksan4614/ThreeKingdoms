using Cysharp.Threading.Tasks;
using System.Collections;
using System.Linq;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    async void Start()
    {
        PopupManager.instance.ShowDimm(true, true);

        await UniTask.WaitForEndOfFrame();

        await TableManager.instance.Initialize();
        await DataManager.userInfo.LoadData();
        await TeamManager.instance.SpawnUpdate();

        PopupManager.instance.ShowDimm(false);
        StageManager.instance.StartStage();
    }
}
