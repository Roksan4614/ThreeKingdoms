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

            //가로가 더 클경우
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
            // 일단 가로부터 줄여주자
            var w = rt.rect.width * rt.localScale.x;
            var pw = rtParent.rect.width * rtParent.localScale.x;
            if (w > pw)
                rt.localScale *= (pw / w);

            // 다음은 세로
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

    public static bool IsNullOrEmpty(this string _string)
        => string.IsNullOrEmpty(_string);
}
