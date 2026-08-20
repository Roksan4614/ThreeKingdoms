using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class Extenssions
{
    public static T GetComponent<T>(this Transform _trns, string _path) where T : Component
    {
        Transform trns = _trns.Find(_path);
        return trns == null ? null : trns.GetComponent<T>();
    }

    public static void ForceRebuildLayout(this Transform _trns, int _count = 0)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_trns);

        if (_count > 0)
        {
            _trns = _trns.parent;
            while (_count > 0 && _trns != null)
            {
                ForceRebuildLayout(_trns);
                _trns = _trns.parent;
                _count--;
            }
        }
    }

    public static GameObject AutoResizeParent(this Transform _obj, bool _isFull = false)
        => AutoResizeParent(_obj.gameObject, _isFull);

    public static GameObject AutoResizeParent(this GameObject _obj, bool _isFull = false)
    {
        var rt = _obj.transform as RectTransform;

        if (rt == null || rt.parent == null)
            return null;

        var rtParent = (RectTransform)rt.parent;

        if (_isFull)
        {
            var w = rt.rect.width * rt.localScale.x;
            var h = rt.rect.height * rt.localScale.y;

            //°¡·Î°¡ ´õ Å¬°æ¿ì
            if (w > h)
            {
                var pw = rtParent.rect.width * rtParent.localScale.x;
                if (w < pw)
                    rt.localScale *= pw / w;
            }
            else
            {
                var ph = rtParent.rect.height * rtParent.localScale.y;
                if (h < ph)
                    rt.localScale *= ph / h;
            }
        }

        {
            // ÀÏ´Ü °¡·ÎºÎÅÍ ÁÙ¿©ÁÖÀÚ
            var w = rt.rect.width * rt.localScale.x;
            var pw = rtParent.rect.width * rtParent.localScale.x;
            if (w > pw)
                rt.localScale *= (pw / w);

            // ´ÙÀ½Àº ¼¼·Î
            var h = rt.rect.height * rt.localScale.y;
            var ph = rtParent.rect.height * rtParent.localScale.y;
            if (h > ph)
                rt.localScale *= (ph / h);
        }
        return _obj;
    }

    public static Transform SetText(this Transform _trns, string _path, object _text, string _default = "", bool _isEnableError = true)
    {
        Transform trns = string.IsNullOrEmpty(_path) == false ? _trns.Find(_path) : _trns;
        if (_text == null)
            _text = "";

        var meshPro = trns?.GetComponent<TextMeshProUGUI>();
        if (meshPro != null)
        {
            meshPro.text = string.IsNullOrEmpty(_text.ToString()) ? _default : _text.ToString();
            return meshPro.transform;
        }
        else
        {
            var text = trns?.GetComponent<Text>();
            if (text != null)
            {
                text.text = string.IsNullOrEmpty(_text.ToString()) ? _default : _text.ToString();
                return text.transform;
            }
        }

        if (_isEnableError == true)
            IngameLog.AddError($"SetText Comp NULL: {_trns.name}: " + _path);

        return null;
    }

    public static string GetHierarchyPath(this Transform _trns)
    {
        string path = _trns.name;
        while (_trns.parent != null)
        {
            _trns = _trns.parent;
            path = _trns.name + "/" + path;
        }

        return path;
    }

    public static string WithJosa(this string _string, bool _isSubject = true)
    {
        if (DataManager.option.language != LanguageType.Korean || _string.IsActive() == false)
            return _string;

        char lastChar = _string[_string.Length - 1];

        // ÇÑ±Û ¹üÀ§ È®ÀÎ (°¡: 0xAC00, ÆR: 0xD7A3)
        if (lastChar < 0xAC00 || lastChar > 0xD7A3) return _string + (_isSubject ? "ÀÌ" : "À»");

        // (±ÛÀÚ - °¡) % 28
        int tailIndex = (lastChar - 0xAC00) % 28;

        return _string + (_isSubject ? (tailIndex == 0 ? "°¡" : "ÀÌ") : (tailIndex == 0 ? "¸¦" : "À»"));
    }
    public static bool IsActive(this string _string)
        => string.IsNullOrWhiteSpace(_string) == false;
    public static bool IsEquals(this string _string, string _value)
        => _string.Equals(_value, StringComparison.Ordinal);

    #region AMOUNT
    public static string AmountKMBT(this int _value, bool _isDot = true, bool _isFullDot = false, bool _isMBT = false, bool _isEastAsia = false)
        => AmountKMBT((double)_value, _isDot, _isFullDot, _isMBT, _isEastAsia);
    public static string AmountKMBT(this long _value, bool _isDot = true, bool _isFullDot = false, bool _isMBT = false, bool _isEastAsia = false)
        => AmountKMBT((double)_value, _isDot, _isFullDot, _isMBT, _isEastAsia);
    public static string AmountKMBT(this float _value, bool _isDot = true, bool _isFullDot = false, bool _isMBT = false, bool _isEastAsia = false)
            => AmountKMBT((double)_value, _isDot, _isFullDot, _isMBT, _isEastAsia);

    public static string AmountKMBT(this double _value, bool _isDot = true, bool _isFullDot = false, bool _isMBT = false, bool _isEastAsia = false)
    {
        string amount = $"{_value:#,##0.##}";

        if ((_isMBT && _value < 1000000) ||
            (_isMBT == false && _value < 1000))
            return _isDot ? (_isFullDot && _value >= 1000) ? $"{_value:#,##0.#0}" : amount : $"{Math.Floor(_value):#,##0}";

        if (_isEastAsia == true)
        {
            switch (DataManager.option.language)
            {
                case LanguageType.Korean:
                    return AmountKMBT_EastAsia(_value, _isDot, _isFullDot);
            }
        }

        var amount_point = amount.Split('.');
        var amount_data = amount_point[0].Split(',');

        var result = amount_data[0];

        if (_isDot == true)
        {
            float valuePoint = int.Parse($"{amount_data[1][0]}{amount_data[1][1]}") * 0.01f;
            result += _isFullDot ? $"{valuePoint:.#0}" : $"{valuePoint:.##}";
        }

        //string keySuffix = amount_data.Length switch
        //{
        //    2 => _isMBT ? "" : "Thousand",
        //    3 => "Million",
        //    4 => "Billion",
        //    5 => "Trilion",
        //    _ => ""
        //};
        //if (keySuffix.IsNullOrEmpty() == false)
        //    result += TableManager.stringTable.GetString("Number_Unit_Suffixes_" + keySuffix);

        string keySuffix = amount_data.Length switch
        {
            2 => _isMBT ? "" : "K",
            3 => "M",
            4 => "B",
            5 => "T",
            _ => ""
        };

        if (keySuffix.IsActive())
            result += keySuffix;

        return result;
    }

    static string AmountKMBT_EastAsia(double _value, bool _isDot = true, bool _isFullDot = false)
    {
        string amount = $"{_value:#,##0.##}";

        List<double> checkDB = new() { 1_000_000_000_000, 100_000_000, 10_000 };
        List<string> symbolDB = new() { "°æ", "¾ï", "¸¸" };

        for (int i = 0; i < checkDB.Count; i++)
        {
            if (_value >= checkDB[i])
            {
                var value = _value / checkDB[i];

                amount = (_isDot == false ? $"{value:0}" : _isFullDot == false ? $"{value:0.##}" : $"{value:0.00}") + symbolDB[i];

                break;
            }
        }

        return amount;
    }
    #endregion AMOUNT

    public static bool Approximately(this float _float, float _value, float _gap = 0.0001f)
        => Mathf.Abs(_float - _value) <= _gap;

    public static string ToRemainTime(this TimeSpan _ts, int _mspace = -1, bool _isDigitS = true, bool _isStringMode = false, bool _isStartMinute = false)
    {
        string result = "";

        if (_isStringMode == true)
        {
            result = _ts.Days > 0 ?
                    $"{_ts.Days}d {_ts.Hours}h" :
                    _ts.Hours > 0 ?
                    $"{_ts.Hours}h {_ts.Minutes}m" :
                    _ts.Minutes > 0 ?
                    $"{_ts.Minutes}m {_ts.Seconds}s" :
                    _ts.Seconds >= 10 ?
                    $"{_ts.Seconds}s" : $"{_ts.TotalSeconds:0.00}s";
        }
        else
        {
            if (_ts.TotalMinutes <= 1)
            {
                result = _ts.Seconds >= 10 ? _ts.Seconds.ToString() : _ts.TotalSeconds.ToString("0.00");

                if (_isDigitS == true)
                    result += "s";
            }
            else if (_isStartMinute || _ts.TotalHours < 1)
                result = $"{Mathf.FloorToInt((float)_ts.TotalMinutes):00}:{_ts.ToString(@"ss")}";
            else
                result = $"{Mathf.FloorToInt((float)_ts.TotalHours):00}:{_ts.ToString(@"mm\:ss")}";

            if (_mspace > -1)
                result = Utils.MSpace(result, _mspace);
        }

        return result;
    }

    #region SORTBY
    private struct KeyComparer<T, V> : IComparer<T> where V : IComparable<V>
    {
        private readonly bool m_isDescending;
        private readonly Func<T, V> m_keySelector;

        public KeyComparer(Func<T, V> _keySelector, bool _isDescending)
        {
            m_keySelector = _keySelector;
            m_isDescending = _isDescending;
        }

        public int Compare(T _x, T _y)
        {
            var x = m_keySelector(_x);
            var y = m_keySelector(_y);

            if (x == null && y == null) return 0;
            if (x == null) return m_isDescending ? 1 : -1;
            if (y == null) return m_isDescending ? -1 : 1;

            return m_isDescending ? y.CompareTo(x) : x.CompareTo(y);
        }
    }
    public static List<T> SortBy<T, V>(this List<T> _source, Func<T, V> _keySelector, bool _isDescending = false) where V : IComparable<V>
    {
        List<T> sortedList = new List<T>(_source);
        sortedList.Sort(new KeyComparer<T, V>(_keySelector, _isDescending));
        return sortedList;
    }
    public static List<T> SortByDescending<T, V>(this List<T> _source, Func<T, V> _keySelector) where V : IComparable<V>
        => _source.SortBy(_keySelector, true);

    public static List<T> SortBy<T, V>(this IReadOnlyList<T> _source, Func<T, V> _keySelector, bool _isDescending = false) where V : IComparable<V>
    {
        List<T> sortedList = new List<T>(_source);
        sortedList.Sort(new KeyComparer<T, V>(_keySelector, _isDescending));
        return sortedList;
    }
    public static List<T> SortByDescending<T, V>(this IReadOnlyList<T> _source, Func<T, V> _keySelector) where V : IComparable<V>
        => _source.SortBy(_keySelector, true);

    public static T[] SortBy<T, V>(this T[] _source, Func<T, V> _keySelector, bool _isDescending = false) where V : IComparable<V>
    {
        T[] sortedArray = new T[_source.Length];
        Array.Copy(_source, sortedArray, _source.Length);
        Array.Sort(sortedArray, new KeyComparer<T, V>(_keySelector, _isDescending));
        return sortedArray;
    }
    public static T[] SortByDescending<T, V>(this T[] _source, Func<T, V> _keySelector) where V : IComparable<V>
        => _source.SortBy(_keySelector, true);

    public static T RandomFirst<T>(this List<T> _source)
        => _source[UnityEngine.Random.Range(0, _source.Count)];
    public static T RandomFirst<T>(this T[] _source)
        => _source[UnityEngine.Random.Range(0, _source.Length)];
    public static T RandomFirst<T>(this IReadOnlyList<T> _source)
        => _source[UnityEngine.Random.Range(0, _source.Count)];
    public static List<T> Shuffle<T>(this IReadOnlyList<T> _source)
    {
        List<T> result = new List<T>(_source);
        return result.Shuffle();
    }
    public static List<T> Shuffle<T>(this List<T> _source)
    {
        List<T> result = new List<T>(_source);
        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    public static T[] Shuffle<T>(this T[] _source)
    {
        T[] result = new T[_source.Length];
        Array.Copy(_source, result, _source.Length);
        for (int i = result.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    #endregion SORTBY

    public static CancellationTokenSource ReleaseCTS(this CancellationTokenSource _cts, bool _isNew = false)
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        return _isNew ? new() : null;
    }
    public static void SetAnchoredPositionX(this RectTransform _rt, float _posX)
        => _rt.SetAnchoredPosition(_posX, null);
    public static void SetAnchoredPositionY(this RectTransform _rt, float _posY)
        => _rt.SetAnchoredPosition(null, _posY);

    public static void SetAnchoredPosition(this RectTransform _rt, float? _posX, float? _posY)
    {
        var pos = _rt.anchoredPosition;
        if (_posX != null)
            pos.x = _posX.Value;
        if (_posY != null)
            pos.y = _posY.Value;

        _rt.anchoredPosition = pos;

    }
    public static void Forget(this Tween _tween, CancellationToken _cancellationToken = default)
        => _tween?.ToUniTask(TweenCancelBehaviour.Kill, _cancellationToken).Forget();

    public static void Alpha(this Image _image, float _alpha)
    {
        Color color = _image.color;
        color.a = _alpha;
        _image.color = color;
    }
}
