using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class WarningAreaComponent : MonoBehaviour, IValidatable
{
    List<CharacterComponent> m_target = new();

    public IReadOnlyList<CharacterComponent> target => m_target;

    public bool Contains(CharacterComponent _target)
        => m_target.Contains(_target);

    enum WarningAnimationType
    {
        Charge,
        Loop,
        End,
    }

    bool m_isShow;
    public async UniTask ShowAsync(float _speed, CancellationToken _token, bool _isDisable = true)
    {
        m_isShow = true;
        m_target.Clear();
        gameObject.SetActive(true);

        Play(WarningAnimationType.Charge);

        float duration = _speed - m_element.lengthLoop;

        m_element.animator.speed = 1 / duration;

        await UniTask.WaitForSeconds(duration, cancellationToken: _token);

        m_element.animator.speed = 1;

        Play(WarningAnimationType.Loop);

        await UniTask.WaitForSeconds(m_element.lengthLoop, cancellationToken: _token);

        Play(WarningAnimationType.End);

        await UniTask.NextFrame();
        await UniTask.WaitForSeconds(m_element.animator.GetCurrentAnimatorStateInfo(0).length, cancellationToken: _token);

        if (_isDisable)
            SetDisable();

        m_isShow = false;
    }

    public void SetDisable()
        => gameObject.SetActive(false);

    void Play(WarningAnimationType _type)
    {
        m_element.animator.CrossFade(_type.ToString(), 0, 0, 0);
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
        {
            var hero = _collision.transform.parent.GetComponent<CharacterComponent>();
            if (hero != null && m_target.Contains(hero) == false)
                m_target.Add(hero);
        }
    }

    private void OnTriggerExit2D(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
        {
            var hero = _collision.transform.parent.GetComponent<CharacterComponent>();
            if (hero != null)
                m_target.Remove(hero);
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Animator animator;
        public float lengthLoop;

        public void Initialize(Transform _transform)
        {
            animator = _transform.GetComponent<Animator>();

            lengthLoop = Array.FindAll(animator.runtimeAnimatorController.animationClips, x => x.name.Contains("_loop", System.StringComparison.OrdinalIgnoreCase))[0].length;
        }
    }
    #endregion VALIDATE

}
