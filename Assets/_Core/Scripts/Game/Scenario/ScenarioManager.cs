using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ScenarioManager
{
    public static ScenarioManager instance { get; private set; } = new();

    AsyncOperationHandle<GameObject> m_handle;

    public void Initialize()
    {
    }

    public static void Release()
    {
        if (instance != null)
        {
            if (instance.m_handle.IsValid() == true)
                instance.m_handle.Release();

            instance = null;
        }
    }

    public async UniTask StartAsync(int _phaseIdx, bool _isStart)
    {
        if (DataManager.option.isScenarioSkip)
            return;

        string stageKey = StageManager.instance.data.GetKey_Scenario(_phaseIdx, _isStart);
        string key = $"Scenario/Scenario_{stageKey}.prefab";

        await AddressableManager.instance.LoadAssetAsync<GameObject>(
            _result =>
            {
                if (_result.Count > 0)
                    m_handle = _result.First().Value;
            }, null, key);

        if (m_handle.IsValid() == false)
            return;

        //await PopupManager.instance.ShowDimmAsync(true, false);

        var scenario = GameObject.Instantiate(m_handle.Result, StageManager.instance.transform);
        await scenario.GetComponent<ScenarioBase>().InitializeAsync(stageKey);

        if (scenario.gameObject.activeSelf == true)
            await PopupManager.instance.ShowDimmAsync(false, _duration: 1f);

        GameObject.Destroy(scenario.gameObject);
        m_handle.Release();
    }
}
