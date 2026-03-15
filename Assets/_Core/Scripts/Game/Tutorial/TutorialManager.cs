using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum TutorialType
{
    START,
}

public class TutorialManager
{
    const string c_key = "Tutorial_Data";
    public static TutorialManager instance { get; private set; } = new();

    List<TutorialType> m_loadData;

    public async UniTask InitializeAsync()
    {
        m_loadData = PPWorker.Get<List<TutorialType>>(c_key);
        if (m_loadData == null)
            m_loadData = new();
    }

    public void Complete(TutorialType _type)
    {
        if (m_loadData.Contains(_type) == false)
        {
            m_loadData.Add(_type);
            SaveData();
        }
    }

    public bool IsComplete(TutorialType _type)
        => m_loadData.Contains(_type);

    void SaveData()
    {
        PPWorker.Set(PlayerPrefsType.TUTORIAL_DATA, m_loadData);
    }

    public async UniTask StartAsync(TutorialType _tutorialType)
    {
        string key = $"Tutorial/Tutorial_{_tutorialType}.prefab";

        AsyncOperationHandle<GameObject> handle = default;
        await AddressableManager.instance.LoadAssetAsync<GameObject>(
            _result =>
            {
                if (_result.Count > 0)
                    handle = _result.First().Value;
            }, null, key);

        if (handle.IsValid() == false)
            return;

        var tutorial = GameObject.Instantiate(handle.Result, StageManager.instance.transform);
        await tutorial.GetComponent<TutorialBase>().StartAsync(_tutorialType);

        GameObject.Destroy(tutorial.gameObject);
        handle.Release();
    }
}
