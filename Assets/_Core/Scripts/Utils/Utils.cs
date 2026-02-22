using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class Utils
{
    public static void ClearDebugLog()
    {
#if UNITY_EDITOR
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        type.GetMethod("Clear").Invoke(new object(), null);
#endif
    }

    public static void AfterSecond(Action _callback, float _duration = 0, CancellationTokenSource _token = null)
    {
        AfterSecondAsync(_callback, _duration, _token).Forget();
    }

    public static async UniTask AfterSecondAsync(Action _callback, float _duration = 0, CancellationTokenSource _token = null)
    {
        await UniTask.WaitForSeconds(_duration, cancellationToken: _token == null ? default : _token.Token);
        _callback();
    }

    public static bool SetOrderInLayer(Transform _trns, OrderLayerType _orderLayer)
    {
        int sortingOrder = (int)_orderLayer;

        var sortingGroup = _trns.GetComponent<SortingGroup>();
        if (sortingGroup != null)
            sortingGroup.sortingOrder = sortingOrder;

        var canvas = _trns.GetComponent<Canvas>();
        if (canvas)
        {
            canvas.sortingOrder = sortingOrder;
            return true;
        }

        var meshRenderer = _trns.GetComponent<MeshRenderer>();
        if (meshRenderer)
        {
            meshRenderer.sortingOrder = sortingOrder;
            return true;
        }

        var particle = _trns.GetComponent<ParticleSystemRenderer>();
        if (particle)
        {
            particle.sortingOrder = sortingOrder;
            return true;
        }

        return false;
    }

    public static MaskableGraphic SetObjectAlpha(GameObject _object, float _alpha, bool _isChild = true)
    {
        return SetObjectAlpha(_object.transform, _alpha, _isChild);
    }

    public static MaskableGraphic SetObjectAlpha(Transform _trns, float _alpha, bool _isChild = true)
    {
        MaskableGraphic mg = null;

        if (_trns.GetComponent<Image>() != null)
            mg = _trns.GetComponent<Image>();
        else if (_trns.GetComponent<Text>() != null)
            mg = _trns.GetComponent<Text>();
        else if (_trns.GetComponent<TextMeshProUGUI>() != null)
            mg = _trns.GetComponent<TextMeshProUGUI>();

        if (mg != null)
        {
            Color clr = mg.color;
            clr.a = _alpha;
            mg.color = clr;
        }

        if (_isChild == true)
        {
            for (int i = 0; i < _trns.childCount; i++)
            {
                SetObjectAlpha(_trns.GetChild(i), _alpha, _isChild);
            }
        }

        return mg;
    }

    public static string FileSize(long _size)
    {
        List<string> strFileSize = new() { "B", "KB", "MB", "GB" };

        int count = 0;
        while (_size >= 1024 && count < strFileSize.Count - 1)
        {
            _size /= 1024;
            count++;
        }

        return $"{_size:0.##} {strFileSize[count]}";
    }
}
