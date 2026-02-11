using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public enum AddressableLabelType
{
    L_Core,
    L_SpriteAtlas,
    L_HeroIcon,

    MAX
}

public partial class AddressableManager : MonoSingleton<AddressableManager>
{
    public string bundleUrl { get; set; }

    protected override void OnAwake()
    {
        Initialize();
    }

    public async void Initialize()
    {
        Addressables.InternalIdTransformFunc = CustomTransform;

        await DoDownloadAsync(null, AddressableLabelType.L_Core, AddressableLabelType.L_SpriteAtlas);
    }

    string CustomTransform(IResourceLocation _location)
    {
        string internalId = _location.InternalId;

        if (internalId.Contains("ROKSAN_Bundle") && bundleUrl.IsNullOrEmpty() == false)
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

        var handle = Addressables.DownloadDependenciesAsync(_keys, Addressables.MergeMode.Union);
        try
        {
            await handle.ToUniTask(progress: _onProgress);
        }
        finally
        {
            handle.Release();
        }
    }

    public async UniTask<long> GetDownloadSizeAsync(params AddressableLabelType[] _keys)
    {
        return await GetDownloadSizeAsync(_keys.Select(_x => _x.ToString()).ToArray());
    }

    public async UniTask<long> GetDownloadSizeAsync(params string[] _keys)
    {
        long totalSize = 0;

        var handle = Addressables.LoadResourceLocationsAsync(_keys, Addressables.MergeMode.Intersection);
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

    public async UniTask LoadAsset<T>(
        UnityAction<Dictionary<string, AsyncOperationHandle<T>>> _onComplete,
        IProgress<float> _onProgress,
        params AddressableLabelType[] _labels)
    {
        await LoadAsset<T>(_onComplete, _onProgress, _labels.Select(_x => _x.ToString()).ToArray());
    }

    public async UniTask LoadAsset<T>(
        UnityAction<Dictionary<string, AsyncOperationHandle<T>>> _onComplete,
        IProgress<float> _onProgress,
        params string[] _keys)
    {
        Dictionary<string, AsyncOperationHandle<T>> resultData = _onComplete == null ? null : new();
        DownloadData downloadData = new();

        downloadData.totalFileSize = await GetDownloadSizeAsync(_keys);

        var handle = Addressables.LoadResourceLocationsAsync(_keys.Select(x => x.ToString()).ToList(), Addressables.MergeMode.Intersection);

        var locations = await handle.ToUniTask();

        if (locations == null)
            IngameLog.Add("Addressable: LoadAsset: Failed: " + string.Join(", ", _keys));
        else if (locations.Count > 0)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                downloadData.fileSize = await Addressables.GetDownloadSizeAsync(locations[i]).ToUniTask();

                var h = Addressables.LoadAssetAsync<T>(locations[i].PrimaryKey);

                try
                {
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
                        if (resultData?.ContainsKey(locations[i].PrimaryKey) == false)
                            resultData.Add(locations[i].PrimaryKey.Split("/").Last().Split(".").First(), h);
                    }
                    else
                        h.Release();
                }
                catch
                {
                    h.Release();
                }
            }
        }

        if (downloadData.totalFileSize > 0)
            _onProgress?.Report(1f);

        handle.Release();
        _onComplete?.Invoke(resultData);
    }

    /*
    public IEnumerator DoLoadAsset<T>(
        UnityAction<Dictionary<string, AsyncOperationHandle<T>>> _onComplete,
        UnityAction<float> _onPercent,
        params AddressableLabelType[] _labels)
    {
        yield return DoLoadAsset<T>(_onComplete, _onPercent, _labels.Select(_x => _x.ToString()).ToArray());
    }

    public IEnumerator DoLoadAsset<T>(
        UnityAction<Dictionary<string, AsyncOperationHandle<T>>> _onComplete,
        UnityAction<float> _onPercent,
        params string[] _keys)
    {
        Dictionary<string, AsyncOperationHandle<T>> resultData = _onComplete == null ? null : new();
        DownloadData downloadData = new();

        yield return DoLoad_DownloadSize(_size => downloadData.totalFileSize = _size, _keys);

        var handle = Addressables.LoadResourceLocationsAsync(_keys.Select(x => x.ToString()).ToList(), Addressables.MergeMode.Intersection);
        yield return handle;

        if (handle.Result != null)
        {
            foreach (var result in handle.Result)
            {
                downloadData.fileSize = Addressables.GetDownloadSizeAsync(result).Result;

                var h = Addressables.LoadAssetAsync<T>(result.PrimaryKey);

                while (h.IsDone == false)
                {
                    if (downloadData.totalFileSize > 0)
                    {
                        downloadData.downloadSize = (long)(downloadData.fileSize * h.PercentComplete);
                        _onPercent?.Invoke((downloadData.totalDownloadSize + downloadData.downloadSize) / downloadData.totalFileSize);
                    }
                    yield return null;
                }

                downloadData.totalDownloadSize += downloadData.fileSize;

                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    if (resultData?.ContainsKey(result.PrimaryKey) == false)
                        resultData.Add(result.PrimaryKey.Split("/").Last().Split(".").First(), h);
                }
                else
                    h.Release();
            }
        }

        if (downloadData.totalFileSize > 0)
            _onPercent?.Invoke(1f);

        handle.Release();
        _onComplete?.Invoke(resultData);
    }
    */

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
