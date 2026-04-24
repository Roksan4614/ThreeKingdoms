using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

public class MissionComponent : Singleton<MissionComponent>, IValidatable
{
    float m_startPosY;

    protected override void OnAwake()
    {
        m_startPosY = m_element.rt.anchoredPosition.y;

        m_element.panel.gameObject.SetActive(false);
    }

    Tween m_tweenMovePanel;
    public void SetMoveArea(bool _isBottom, bool _isTween = true, float _duration = .2f)
    {
        m_tweenMovePanel?.Kill();
        float target = _isBottom ? 270 : m_startPosY;
        m_tweenMovePanel = m_element.rt.DOAnchorPosY(target, _duration);
    }

    #region VALIDATE
    public void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public RectTransform rt;
        public Transform panel;

        public void Initialize(Transform _transform)
        {
            rt = (RectTransform)_transform;
            panel = rt.Find("Panel");
        }
    }
    #endregion VALIDATE
}
