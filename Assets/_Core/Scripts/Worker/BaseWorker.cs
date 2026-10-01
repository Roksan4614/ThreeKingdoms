using System;
using System.Collections.Generic;
using UnityEngine;

public class WorkerManager
{
    static WorkerManager m_instance;
    public static WorkerManager instance => m_instance ??= new();

    List<IWorker> m_singleton = new();

    public void Add(IWorker _worker)
        => m_singleton.Add(_worker);

    public static void Release()
    {
        for (int i = 0; i < m_instance.m_singleton.Count; i++)
            m_instance.m_singleton[i].ReleaseWorker();
        m_instance = null;
    }
}


public interface IWorker
{
    void ReleaseWorker();
    void OnRelease();
}

public class BaseWorker<T> : IWorker where T : class, IWorker, new()
{
    static T m_instance;
    public static T instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new();
                WorkerManager.instance.Add(m_instance);
            }
            return m_instance;
        }
    }
    public static void Release()
    {
        if (m_instance != null)
        {
            try
            {
                m_instance.OnRelease();
            }
            catch (Exception e)
            {
                IngameLog.Add("WorkerRelease Error: " + e.Message);
            }

            m_instance = null;
        }
    }

    public void ReleaseWorker()
    {
        Release();
    }

    public virtual void OnRelease()
    {
    }
}
