using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
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

    #region AMOUNT
    public static string AmountKMBT(this int _value, bool _isDot = true, bool _isFullDot = false, bool _isMBT = false)
        => AmountKMBT((double)_value, _isDot, _isFullDot, _isMBT);
    public static string AmountKMBT(this long _value, bool _isDot = true, bool _isFullDot = false, bool _isMBT = false)
        => AmountKMBT((double)_value, _isDot, _isFullDot, _isMBT);

    public static string AmountKMBT(this double _value, bool _isDot = true, bool _isFullDot = false, bool _isMBT = false)
    {
        string amount = $"{_value:#,##0.##}";

        if ((_isMBT && _value < 1000000) ||
            (_isMBT == false && _value < 1000))
            return _isDot ? _isFullDot ? $"{_value:#,##0.#0}" : amount : $"{Math.Floor(_value):#,##0}";

        switch (DataManager.option.language)
        {
            case LanguageType.Korean:
                return AmountKMBT_EastAsia(_value);
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
}
