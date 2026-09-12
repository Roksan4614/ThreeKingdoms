using Cysharp.Threading.Tasks;
using Rev9.Post;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;

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
        List<UniTask> tasks = new();
        tasks.Add(m_stat.InitializeAsync());
        tasks.Add(m_heroPosition.InitializeAsync());
        tasks.Add(m_castle.InitializeAsync());
        tasks.Add(m_bossRaid.InitializeAsync());
        tasks.Add(dailyDungeon.InitializeAsync());
        tasks.Add(storyMode.InitializeAsync());

        tasks.Add(InventoryWorker.instance.InitializeAsync());
        tasks.Add(PostWorker.instance.InitializeAsync());
        tasks.Add(QuestWorker.instance.InitializeAsync());

        await StartupTasks.WaitAllAsync(tasks);
    }

    public static void Release()
    {
        var previous = m_instance;
        if (previous == null) return;
        var failures = new List<Exception>();
        try { previous.m_castle.Release(); } catch (Exception error) { failures.Add(error); }
        try { previous.m_bossRaid.ReleaseCTS(); } catch (Exception error) { failures.Add(error); }
        finally { m_instance = null; }
        if (failures.Count > 0) throw new AggregateException("Data cleanup failed", failures);
    }

    public bool isLobby => AddressableManager.instance.curSceneName.Contains("Lobby");
}
