using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;

public class DataManager
{
    public static DataManager instance { get; private set; } = new();

    public static Data_UserInfo userInfo { get; private set; } = new();
    public static Data_Option option { get; private set; } = new();

    public static async UniTask Initialize()
    {
        await userInfo.Initialize();
        option.Initialize();
    }
}
