using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

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
}
