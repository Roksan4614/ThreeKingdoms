using UnityEngine;

public class DataManager
{
    public static DataManager instance { get; private set; } = new();

    public static Data_UserInfo userInfo { get; private set; } = new();
}
