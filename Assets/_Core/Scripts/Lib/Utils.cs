using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

    public static void AfterSecond(Action _callback, float _duration = 0, CancellationToken _token = default)
    {
        AfterSecondAsync(_callback, _duration, _token).Forget();
    }

    public static async UniTask AfterSecondAsync(Action _callback, float _duration = 0, CancellationToken _token = default)
    {
        await UniTask.WaitForSeconds(_duration, cancellationToken: _token);
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

    public static void WaitEscape(MonoBehaviour _mono, UnityAction _onEscape, bool _isForceBreak = false, CancellationToken _token = default)
    {
        WaitEscapeAsync(_mono, _onEscape, _isForceBreak, _token).Forget();
    }

    public static async UniTask WaitEscapeAsync(MonoBehaviour _mono, UnityAction _onEscape, bool _isForceBreak = false, CancellationToken _token = default)
    {
        while (true)
        {
            await UniTask.WaitUntil(() =>
            {
                return Input.GetKeyDown(KeyCode.Escape);
            }, cancellationToken: _token == default ? _mono.destroyCancellationToken : _token);

            if (PopupManager.instance.isOpenModal)
            {
                if (PopupManager.instance.lastPopupModal.isSwitchEscape)
                    PopupManager.instance.lastPopupModal.Close();
                continue;
            }

            _onEscape();

            if (_isForceBreak == true)
                break;
        }
    }

    public static string MSpace(string _msgAmount, int _mspace = 20)
    {
        List<char> ignores = new string[]
        { ",", ".", "%", ":" }
        .SelectMany(x => x.ToCharArray()).ToList();

        var msgs = _msgAmount.Split(ignores.ToArray());

        List<char> ignoreChar = new();
        foreach (char c in _msgAmount)
        {
            if (ignores.Contains(c))
                ignoreChar.Add(c);
        }

        int index = 0;
        string result = "";
        foreach (var msg in msgs)
        {
            if (msg.IsActive())
                result += $"<mspace={_mspace}>{msgs[index]}</mspace>";
            result += $"{(ignoreChar.Count > index ? ignoreChar[index++] : "")}";
        }

        return result;
    }

    public static void SetActivePunch(Transform _transform, bool _isActive, bool _isAutoActive = true)
        => SetActivePunchAsync(_transform, _isActive, _isAutoActive).Forget();
    public static async UniTask SetActivePunchAsync(Transform _transform, bool _isActive, bool _isAutoActive = true)
    {
        if (_isActive == true && _isAutoActive == true)
            _transform.gameObject.SetActive(true);

        var targetScale = Vector3.one * (_isActive ? 1 : 0.5f);
        if (_isActive)
            _transform.localScale = Vector3.one * 0.5f;
        var duration = 0.15f;

        await _transform.DOScale(targetScale, duration).SetEase(_isActive ? Ease.OutBack : Ease.InBack).AsyncWaitForCompletion();

        if (_isActive == false && _isAutoActive == true)
            _transform.gameObject.SetActive(false);
    }

    public static void Shake(Transform _trns, bool _isForceShake = false, float strength = 10f, int vibrato = 1)
        => ShakeAsync(_trns, _isForceShake, strength, vibrato).Forget();
    public static async UniTask ShakeAsync(Transform _trns, bool _isForceShake = false, float strength = 10f, int vibrato = 1)
    {
        if (ControllerManager.instance.isDoing == true && _isForceShake == false)
            return;

        int count = 3;
        while (count > 0)
        {
            await _trns.DOShakePosition(.05f, strength, vibrato).AsyncWaitForCompletion();
            count--;
        }
    }

    public static DateTime GetUTC(bool _isServerTime = true)
    {
        if (_isServerTime)
            return DateTime.UtcNow.AddSeconds(Configure.instance.timeGapFromServer);
        return DateTime.UtcNow;
    }

    // 서버 시간이 더 빠르다면, 빠른만큼 빼줘야 로컬과 계산이 맞는다.
    public static DateTime DateTimeParse(string _msgTime, bool _isFromServerTime = true)
        => _isFromServerTime ?
        DateTime.Parse(_msgTime).AddSeconds(-Configure.instance.timeGapFromServer) :
        DateTime.Parse(_msgTime);

    public static string GetRemainTime(TimeSpan _ts, bool _isDay = false)
    {
        string remain = "";

        if (_ts.Days > 0 && _isDay == true)
            remain += $"{_ts.Days}{_ts.Hours}:{_ts.Minutes:00}";
        else if (_ts.Hours > 0)
            remain += $"{_ts.Hours}:{_ts.Minutes:00}";
        else if (_ts.Minutes > 0)
            remain += $"{_ts.Minutes}:{_ts.Seconds:00}";
        else
            remain += $"{_ts.Seconds}:{_ts.Milliseconds:00}";

        return remain;
    }
}
