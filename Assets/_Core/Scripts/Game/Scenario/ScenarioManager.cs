using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ScenarioManager
{
    public static ScenarioManager instance { get; private set; } = new();



    public void Initialize()
    {

    }

    public async UniTask StartAsync(int _phaseIdx, bool _isStart)
    {
        if (DataManager.option.isScenarioSkip)
            return;

        string key = GetStageData().GetKey(_phaseIdx, _isStart);

        AsyncOperationHandle<GameObject> handle = default;
        await AddressableManager.instance.LoadAssetAsync<GameObject>(
            _result =>
            {
                if (_result.Count > 0)
                    handle = _result.First().Value;
            }, null, key);

        if (handle.IsValid() == false)
            return;

        handle.Release();
    }

    public StageManager.LoadData_Stage GetStageData()
        => StageManager.instance.data;
}
