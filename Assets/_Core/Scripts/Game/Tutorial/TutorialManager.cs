using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum TutorialType
{
    START,

    CASTLE_START,
    CASTLE_WALLY,
    CASTLE_FINISHED,
}

public class TutorialManager
{
    public static TutorialManager instance { get; private set; } = new();

    List<TutorialType> m_loadData;

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        m_loadData = PPWorker.Get<List<TutorialType>>(PlayerPrefsType.TUTORIAL_DATA);
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

    //public static async UniTask WaitComplete(TutorialType _type, CancellationToken _token)
    //    => await UniTask.WaitUntil(() => instance.IsComplete(_type), cancellationToken: _token);

    //public bool IsComplete(TutorialType _type)
    //    => m_loadData.Contains(_type);

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
        tutorial.transform.position = Vector3.zero;
        await tutorial.GetComponent<TutorialBase>().StartAsync(_tutorialType).SuppressCancellationThrow();

        await UniTask.WaitUntil(() => PopupManager.instance.isTweenDimm == false);

        TeamManager.instance.RemoveBuff(BuffType.NONE);

        GameObject.Destroy(tutorial.gameObject);
        handle.Release();
    }
}
