using Cysharp.Threading.Tasks;
using DG.Tweening;
using Mono.Cecil.Cil;
using System;
using System.Collections;
using UnityEngine;

public class Character_Woker_Move : Character_Worker
{
    public Character_Woker_Move(CharacterComponent _owner) : base(_owner)
    {
    }
    public bool isMoving => m_owner.rig.linearVelocity == Vector2.zero;
    public bool isFlip => m_owner.panel.localScale.x < 0;

    public void MoveStop()
    {
        m_owner.rig.linearVelocity = Vector2.zero;
        m_owner.anim.Play(CharacterAnimType.Idle);

        if (m_coMoveTarget != null)
        {
            m_owner.StopCoroutine(m_coMoveTarget);
            m_coMoveTarget = null;
        }
    }

    public void OnMoveUpdate(Vector2 _velocity)
    {
        if (_velocity == Vector2.zero)
            return;

        if (m_tweenDash == null)
        {
            if (m_owner.anim.IsType(CharacterAnimType.Walk) == false)
                m_owner.anim.Play(CharacterAnimType.Walk);

            m_owner.rig.linearVelocity = _velocity;
        }

        if (_velocity.x != 0)
            SetFlip(_velocity.x > 0);
    }

    public void SetFlip(bool _isFlip)
    {
        if (_isFlip == m_owner.panel.localScale.x > 0)
        {
            var scale = m_owner.panel.localScale;
            scale.x *= -1;
            m_owner.panel.localScale = scale;
        }
    }

    Coroutine m_coMoveTarget;
    public void MoveTarget(CharacterComponent _target, bool _isAttack)
    {
        if (m_coMoveTarget != null)
            m_owner.StopCoroutine(m_coMoveTarget);

        m_coMoveTarget = m_owner.StartCoroutine(DoMoveTarget(_target, _isAttack));
    }

    public IEnumerator DoMoveTarget(CharacterComponent _target, bool _isAttack)
    {
        while (_target.isLive)
        {
            var lookAt = _target.transform.position - m_owner.transform.position;
            OnMoveUpdate(lookAt.normalized * m_owner.data.moveSpeed);

            if (_isAttack && m_owner.target.Contains(_target))
            {
                m_owner.target.SetTarget(_target);
                yield return m_owner.attack.DoAttack();

                if (m_owner.target.Contains(_target) == false)
                    yield break;
            }

            yield return null;
        }

        m_coMoveTarget = null;
    }

    Tween m_tweenDash;
    public void Dash(Vector3 _targetPos)
        => DashAsync(_targetPos).Forget();
    public async UniTask DashAsync(Vector3 _targetPos)
    {
        //test
        if (m_tweenDash == null)
            return;

        m_tweenDash?.Kill();

        Vector3 lookAt = _targetPos == Vector3.zero ? m_owner.rig.linearVelocity : _targetPos;
        Vector3 target = lookAt.normalized * 5;

        if (_targetPos != Vector3.zero)
            target += m_owner.transform.position;

        DateTime dt = DateTime.Now.AddSeconds(0.1f);
        EffectWorker.instance.Dash(m_owner, isFlip);
        m_owner.anim.Play(CharacterAnimType.Dash);

        m_tweenDash = DOTween.To(() => m_owner.transform.position, _pos => m_owner.rig.MovePosition(_pos), target, 0.2f);
        await m_tweenDash.OnUpdate(
            () =>
            {
                if (DateTime.Now > dt)
                {
                    EffectWorker.instance.Dash(m_owner, isFlip);
                    dt = DateTime.Now.AddSeconds(10);
                }
            }).AsyncWaitForCompletion();

        m_tweenDash = null;
    }
}
