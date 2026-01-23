using UnityEngine;

public class DataManager
{
    static DataManager m_instance;
    public static DataManager instance => m_instance ?? new();

    public static Data_UserInfo userInfo { get; private set; } = new();
}
