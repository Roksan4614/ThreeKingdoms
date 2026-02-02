using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class Utils
{
    public static void AfterCoroutine(Action _callback, float _duration = 0)
    {
        PopupManager.instance.StartCoroutine(DoAfterCoroutine(_callback, _duration));
    }

    public static IEnumerator DoAfterCoroutine(Action _callback, float _duration = 0)
    {
        if (_duration == 0)
            yield return null;
        else
            yield return new WaitForSeconds(_duration);

        _callback.Invoke();
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
}
