using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

public enum AddressableLabelType
{
    L_Core,
    L_SpriteAtlas,
    L_HeroIcon,
    L_TableData,

    MAX
}

public partial class AddressableManager : MonoSingleton<AddressableManager>
{
    public string bundleUrl { get; set; }

    Dictionary<string, AsyncOperationHandle<SpriteAtlas>> m_loadedAtlas = new();

    protected override void OnAwake()
    {
        bundleUrl = "https://dev-static.kingz.app/Bundle/WebGL/" + Application.version.Split('.')[2];
    }

    private void OnApplicationQuit()
    {
        Signal.Release();
        DataManager.Release();
        ScenarioManager.Release();
        TimeManager.instance.Release();
        AdsManager.instance.Release();
        TournamentWorker.instance.Release();
    }

    public async UniTask InitializeAsync()
    {
        Addressables.InternalIdTransformFunc = CustomTransform;

        await DoDownloadAsync(null, AddressableLabelType.L_Core, AddressableLabelType.L_SpriteAtlas);

        SpriteAtlasManager.atlasRequested += (string _tag, Action<SpriteAtlas> _callback) =>
        {
            if (m_loadedAtlas.ContainsKey(_tag))
            {
                _callback?.Invoke(m_loadedAtlas[_tag].Result);
                return;
            }

            string key = $"Atlas/{_tag}.spriteatlasv2";
            IngameLog.Add("SpriteAtlasManager.atlasRequested: " + key);
            LoadAssetAsync<SpriteAtlas>(_result =>
            {
                foreach (var s in _result)
                {
                    if (m_loadedAtlas.ContainsKey(_tag) == false)
                        m_loadedAtlas.Add(_tag, s.Value);
                    _callback?.Invoke(s.Value.Result);
                }


            }, null, key).Forget();

            //Addressables.LoadAssetAsync<SpriteAtlas>(_tag).Completed += handle =>
            //{
            //    if (handle.Status == AsyncOperationStatus.Succeeded)
            //        _callback?.Invoke(handle.Result);
            //};
        };
    }

    string CustomTransform(IResourceLocation _location)
    {
        string internalId = _location.InternalId;

        if (internalId.Contains("ROKSAN_Bundle") && bundleUrl.IsActive())
            internalId = internalId.Replace("ROKSAN_Bundle", bundleUrl);

        return internalId;
    }

    public async UniTask DoDownloadAsync(IProgress<float> _onProgress, params AddressableLabelType[] _labels)
    {
        await DoDownloadAsync(_onProgress, _labels.Select(_x => _x.ToString()).ToArray());
    }

    public async UniTask DoDownloadAsync(IProgress<float> _onProgress, params string[] _keys)
    {
        string logKey = string.Join(",", _keys);

        var handle = Addressables.DownloadDependenciesAsync(_keys, m_mergeMode);
        try
        {
            await handle.ToUniTask(progress: _onProgress);
        }
        finally
        {
            handle.Release();
        }
    }

    public async UniTask<bool> HasData(string _key)
    {
        var location = await Addressables.LoadResourceLocationsAsync(_key, m_mergeMode).ToUniTask();

        return location != null && location.Count > 0;
    }

    public async UniTask<long> GetDownloadSizeAsync(params AddressableLabelType[] _keys)
    {
        var result = await GetDownloadSizeAsync(_keys.Select(_x => _x.ToString()).ToArray());
        return result;
    }

    public async UniTask<long> GetDownloadSizeAsync(params string[] _keys)
    {
        long totalSize = 0;

        var handle = Addressables.LoadResourceLocationsAsync(_keys, m_mergeMode);
        try
        {
            var locations = await handle.ToUniTask();

            if (locations == null)
                IngameLog.Add("Addressable: GetDownloadSize: Failed: " + string.Join(", ", _keys));
            else if (locations.Count > 0)
            {
                var tasks = new UniTask<long>[locations.Count];

                for (var i = 0; i < tasks.Length; i++)
                    tasks[i] = Addressables.GetDownloadSizeAsync(locations[i]).ToUniTask();

                long[] sizes = await UniTask.WhenAll(tasks);

                foreach (var size in sizes)
                    totalSize += size;
            }
        }
        finally
        {
            handle.Release();
        }

        return totalSize;
    }

    //public IEnumerator DoDownload(UnityAction<float> _onPercent, params AddressableLabelType[] _labels)
    //{
    //    yield return DoDownload(_onPercent, _labels.Select(_x => _x.ToString()).ToArray());
    //}

    //public IEnumerator DoDownload(UnityAction<float> _onPercent, params string[] _keys)
    //{
    //    string logKey = string.Join(",", _keys);
    //    IngameLog.Add("DoDownload_Scene: Start: " + logKey);
    //    var handle = Addressables.DownloadDependenciesAsync(_keys, true);

    //    while (handle.IsDone == false)
    //    {
    //        _onPercent?.Invoke(handle.PercentComplete);
    //        yield return null;
    //    }

    //    IngameLog.Add("DoDownload_Scene: Finished: " + logKey);
    //}

    //public IEnumerator DoLoad_DownloadSize(UnityAction<long> _onComplete, params AddressableLabelType[] _labels)
    //{
    //    yield return DoLoad_DownloadSize(_onComplete, _labels.Select(_x => _x.ToString()).ToArray());
    //}

    //public IEnumerator DoLoad_DownloadSize(UnityAction<long> _onComplete, params string[] _keys)
    //{
    //    long totalSize = 0;

    //    var handle = Addressables.LoadResourceLocationsAsync(_keys, Addressables.MergeMode.Intersection);
    //    yield return handle;

    //    if (handle.Result == null)
    //        IngameLog.Add("Addressable: GetDownloadSize: Failed: " + string.Join(", ", _keys));
    //    else
    //    {
    //        foreach (var result in handle.Result)
    //        {
    //            if (result.PrimaryKey != _keys[0].ToString())
    //                totalSize += Addressables.GetDownloadSizeAsync(result).Result;
    //        }
    //    }

    //    handle.Release();
    //    _onComplete(totalSize);
    //}

    //이 함수는 동시에 하면 좃됨.. 하나의 라벨
    public async UniTask LoadAssetIntersectionAsync<T>(
        UnityAction<Dictionary<string, AsyncOperationHandle<T>>> _onComplete,
        IProgress<float> _onProgress,
        params AddressableLabelType[] _labels)
    {
        m_mergeMode = Addressables.MergeMode.Intersection;
        await LoadAssetAsync<T>(_onComplete, _onProgress, _labels.Select(_x => _x.ToString()).ToArray());
    }

    public async UniTask LoadAssetAsync<T>(
        UnityAction<Dictionary<string, AsyncOperationHandle<T>>> _onComplete,
        IProgress<float> _onProgress,
        params AddressableLabelType[] _labels)
    {
        await LoadAssetAsync<T>(_onComplete, _onProgress, _labels.Select(_x => _x.ToString()).ToArray());
    }

    bool m_isLogSwitch;
    public void OnLogSwitch() => m_isLogSwitch = true;
    Addressables.MergeMode m_mergeMode = Addressables.MergeMode.Union; // 포함한거 전체
    public async UniTask LoadAssetAsync<T>(
        UnityAction<Dictionary<string, AsyncOperationHandle<T>>> _onComplete,
        IProgress<float> _onProgress,
        params string[] _keys)
    {
        bool isLogSwitch = m_isLogSwitch;
        m_isLogSwitch = false;

        if (isLogSwitch)
            IngameLog.Add("Addressable: LoadAsset: " + _keys[0]);

        Dictionary<string, AsyncOperationHandle<T>> resultData = _onComplete == null ? null : new();
        DownloadData downloadData = new();

        if (isLogSwitch)
            IngameLog.Add("Addressable: totalFileSize: Start");

        //downloadData.totalFileSize = await GetDownloadSizeAsync(_keys);

        //if (isLogSwitch)
        //    IngameLog.Add("Addressable: totalFileSize: " + downloadData.totalFileSize);

        var handle = Addressables.LoadResourceLocationsAsync(_keys.Select(x => x.ToString()).ToList(), m_mergeMode);

        if (isLogSwitch)
            IngameLog.Add("Addressable: LoadAsset: HANDLE CHECK: " + handle.IsValid());

        var locations = await handle.ToUniTask();

        if (isLogSwitch)
            IngameLog.Add("Addressable: LoadAsset: HANDLE CHECK: FINISHED");

        if (locations == null)
        {
            if (isLogSwitch)
                IngameLog.Add("Addressable: LoadAsset: Failed: " + string.Join(", ", _keys));
        }
        else if (locations.Count > 0)
        {
            // 다운로드 총 파일용량 구하기
            {
                var tasks = new UniTask<long>[locations.Count];

                for (var i = 0; i < tasks.Length; i++)
                    tasks[i] = Addressables.GetDownloadSizeAsync(locations[i]).ToUniTask();

                long[] sizes = await UniTask.WhenAll(tasks);

                foreach (var size in sizes)
                    downloadData.totalFileSize += size;
            }

            if (isLogSwitch)
                IngameLog.Add("Addressable: LoadAsset: Start: " + locations.Count);

            {
                var tasks = new List<UniTask>();
                for (int i = 0; i < locations.Count; i++)
                    tasks.Add(LoadAssetParallel(locations[i]));

                await UniTask.WhenAll(tasks);
            }

            async UniTask LoadAssetParallel(IResourceLocation _location)
            {
                downloadData.fileSize = await Addressables.GetDownloadSizeAsync(_location).ToUniTask();

                var h = Addressables.LoadAssetAsync<T>(_location.PrimaryKey);

                try
                {
                    if (isLogSwitch)
                        IngameLog.Add("Addressable: LoadAsset: Parallel: " + _location.PrimaryKey + ": Start");

                    var result = await h.ToUniTask(progress: Progress.Create<float>(_p =>
                    {
                        if (downloadData.totalFileSize > 0)
                        {
                            downloadData.downloadSize = (long)(downloadData.fileSize * h.PercentComplete);
                            _onProgress?.Report((downloadData.totalDownloadSize + downloadData.downloadSize) / (float)downloadData.totalFileSize);
                        }
                    }));

                    downloadData.totalDownloadSize += downloadData.fileSize;

                    if (h.Status == AsyncOperationStatus.Succeeded)
                    {
                        if (isLogSwitch)
                            IngameLog.Add("Addressable: LoadAsset: Succeeded: " + _location.PrimaryKey);

                        string resultKey = _location.PrimaryKey.Split("/").Last().Split(".").First();
                        if (resultData?.ContainsKey(resultKey) == false)
                            resultData.Add(resultKey, h);
                    }
                    else
                    {
                        IngameLog.Add("Addressable: LoadAsset: Failed: " + _location.PrimaryKey);
                        h.Release();
                    }
                }
                catch (Exception _e)
                {
                    IngameLog.Add("Addressable: LoadAsset: Failed: try catch error: " + _location.PrimaryKey + ": " + _e.Message);
                    h.Release();
                }

                if (isLogSwitch)
                    IngameLog.Add("Addressable: LoadAsset: Finished: " + _location.PrimaryKey);
            }
        }

        if (downloadData.totalFileSize > 0)
            _onProgress?.Report(1f);

        handle.Release();
        _onComplete?.Invoke(resultData);
    }

    UnityEngine.ResourceManagement.ResourceProviders.SceneInstance m_prevSceneInstance;
    async UniTask LoadSceneAsync(string _sceneName)
    {
        if (m_prevSceneInstance.Scene.IsValid())
            await Addressables.UnloadSceneAsync(m_prevSceneInstance).ToUniTask();

        curSceneName = _sceneName;
        m_prevSceneInstance = await Addressables.LoadSceneAsync(_sceneName).ToUniTask();
    }
    public string curSceneName { get; private set; }

    public void LoadScene(string _sceneName)
        => LoadSceneAsync(_sceneName).Forget();

    void Release(AsyncOperationHandle _h)
    {
        if (_h.IsValid())
            _h.Release();
    }


    public struct DownloadData
    {
        public long totalFileSize;
        public long totalDownloadSize;

        public long fileSize;
        public long downloadSize;

        //custom
        public string _totalFileSize => Utils.FileSize(totalFileSize);
        public string _totalDownloadSize => Utils.FileSize(totalDownloadSize);
        public string _fileSize => Utils.FileSize(fileSize);
        public string _downloadSize => Utils.FileSize(downloadSize);
    }
}
