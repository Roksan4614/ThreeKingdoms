using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DataManager
{
    static DataManager m_instance;

    public static DataManager instance
    {
        get
        {
            if (m_instance == null)
                m_instance = new();
            return m_instance;
        }
    }

    Data_UserInfo m_userInfo = new();
    Data_Option m_option = new();
    Data_Stat m_stat = new();
    Data_HeroPosition m_heroPosition = new();
    Data_Castle m_castle = new();
    Data_BossRaid m_bossRaid = new();
    Data_DailyDungeon m_dailyDungeon = new();
    Data_StoryMode m_storyMode = new();

    public static Data_UserInfo userInfo => instance.m_userInfo;
    public static Data_Option option => instance.m_option;
    public static Data_Stat stat => instance.m_stat;
    public static Data_HeroPosition heroPosition => instance.m_heroPosition;
    public static Data_Castle castle => instance.m_castle;
    public static Data_BossRaid bossRaid => instance.m_bossRaid;
    public static Data_DailyDungeon dailyDungeon => instance.m_dailyDungeon;
    public static Data_StoryMode storyMode => instance.m_storyMode;

    public async UniTask InitializeAsync()
    {
        await m_userInfo.InitializeAsync();

        List<UniTask> tasks = new();
        tasks.Add(m_stat.InitializeAsync());
        tasks.Add(m_heroPosition.InitializeAsync());
        tasks.Add(m_castle.InitializeAsync());
        tasks.Add(m_bossRaid.InitializeAsync());
        tasks.Add(dailyDungeon.InitializeAsync());
        tasks.Add(storyMode.InitializeAsync());

        await UniTask.WhenAll(tasks);
    }

    public static void Release()
    {
        if (m_instance != null)
        {
            m_instance.m_castle.Release();

            m_instance = null;
        }

        bossRaid.ReleaseCTS();
    }
}
