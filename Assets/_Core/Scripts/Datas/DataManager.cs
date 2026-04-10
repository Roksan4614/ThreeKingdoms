using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DataManager
{
    public static DataManager instance { get; private set; } = new();

    public static Data_UserInfo userInfo { get; private set; } = new();
    public static Data_Option option { get; private set; } = new();
    public static Data_Stat stat { get; private set; } = new();

    public static async UniTask InitializeAsync()
    {
        //List<UniTask> tasks = new();
        //tasks.Add(userInfo.InitializeAsync());
        //tasks.Add(stat.InitializeAsync());

        //await UniTask.WhenAll(tasks);

        await userInfo.InitializeAsync();
        await stat.InitializeAsync();
    }

    public static void Release()
    {
        instance = null;
        userInfo = null;
        option = null;
        stat = null;
    }
}
