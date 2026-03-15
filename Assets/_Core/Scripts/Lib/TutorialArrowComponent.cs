using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

public class TutorialArrowComponent : MonoBehaviour, IValidatable
{
    float duration = .5f;
    float waitTime = 1f;

    private void OnEnable()
    {
        ActionAsync().Forget();
    }

    async UniTask ActionAsync()
    {
        var prevPos = m_element.rt.anchoredPosition;
        var startPos = prevPos;
        startPos.y -= 50 * m_element.rt.localScale.y;

        while (gameObject?.activeSelf == true)
        {
            m_element.rt.anchoredPosition = startPos;
            await m_element.rt.DOAnchorPosY(prevPos.y, duration).SetEase(Ease.OutCubic).AsyncWaitForCompletion();

            await UniTask.WaitForSeconds(waitTime, cancellationToken: destroyCancellationToken);
        }
        m_element.rt.anchoredPosition = prevPos;
    }

    public void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public RectTransform rt;
        public void Initialize(Transform _transform)
        {
            rt = (RectTransform)_transform;
        }
    }
}
