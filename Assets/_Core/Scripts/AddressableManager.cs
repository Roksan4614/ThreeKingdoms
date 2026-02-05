using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public enum AddressableLabelType
{
    L_Core,
    L_SpriteAtlas,

    MAX
}

public class AddressableManager
{
    static AddressableManager m_instance;
    public static AddressableManager instance
    {
        get
        {
            if (m_instance == null)
                m_instance = new AddressableManager(); return m_instance;
        }
    }

    public string bundleUrl { get; set; }

    public IEnumerator DoInitialize()
    {
        Addressables.InternalIdTransformFunc = CustomTransform;

        yield return DoDownload(null, AddressableLabelType.L_Core, AddressableLabelType.L_SpriteAtlas);
    }

    string CustomTransform(IResourceLocation _location)
    {
        string internalId = _location.InternalId;

        if (internalId.Contains("ROKSAN_Bundle") && bundleUrl.IsNullOrEmpty() == false)
            internalId = internalId.Replace("ROKSAN_Bundle", bundleUrl);

        return internalId;
    }

    public IEnumerator DoDownload(UnityAction<float> _onPercent, params AddressableLabelType[] _labels)
    {
        yield return DoDownload(_onPercent, _labels.Select(_x => _x.ToString()).ToArray());
    }

    public IEnumerator DoDownload(UnityAction<float> _onPercent, params string[] _keys)
    {
        string logKey = string.Join(",", _keys);
        IngameLog.Add("DoDownload_Scene: Start: " + logKey);
        var handle = Addressables.DownloadDependenciesAsync(_keys, true);

        while (handle.IsDone == false)
        {
            _onPercent?.Invoke(handle.PercentComplete);
            yield return null;
        }

        IngameLog.Add("DoDownload_Scene: Finished: " + logKey);
    }

    public IEnumerator DoLoad_DownloadSize(UnityAction<long> _onComplete, params AddressableLabelType[] _labels)
    {
        yield return DoLoad_DownloadSize(_onComplete, _labels.Select(_x => _x.ToString()).ToArray());
    }

    public IEnumerator DoLoad_DownloadSize(UnityAction<long> _onComplete, params string[] _keys)
    {
        long totalSize = 0;

        var handle = Addressables.LoadResourceLocationsAsync(_keys, Addressables.MergeMode.Intersection);
        yield return handle;

        if (handle.Result == null)
            IngameLog.Add("Addressable: GetDownloadSize: Failed: " + string.Join(", ", _keys));
        else
        {
            foreach (var result in handle.Result)
            {
                if (result.PrimaryKey != _keys[0].ToString())
                    totalSize += Addressables.GetDownloadSizeAsync(result).Result;
            }
        }

        handle.Release();
        _onComplete(totalSize);
    }

    public IEnumerator DoLoadAsset<T>(
        UnityAction<Dictionary<string, AsyncOperationHandle<T>>> _onComplete,
        UnityAction<float> _onPercent,
        params string[] _key)
    {
        yield return null;
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
