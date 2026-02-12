using Cysharp.Threading.Tasks;
using System.Collections;
using System.Linq;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    async void Start()
    {
        await TableManager.instance.Initialize();
        await DataManager.userInfo.LoadData();
        await TeamManager.instance.SpawnUpdate();

        StageManager.instance.StartStage();
    }
}
