using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class Character_Worker_Move : Character_Worker
{
    public Character_Worker_Move(CharacterComponent _owner) : base(_owner)
    {
    }

    public bool isDash => m_tweenDash != null;

    public bool isMoving => m_owner.rig.linearVelocity == Vector2.zero;
    public bool isFlip => m_owner.panel.localScale.x < 0; // 오른쪽을 보는게 플립임. 기본이 왼쪽보니까

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

    public void OnMoveUpdate(Vector2 _velocity, bool _isAnim = true)
    {
        if (_velocity == Vector2.zero)
            return;

        if (m_tweenDash == null)
        {
            if (_isAnim == true &&
                m_owner.anim.IsType(CharacterAnimType.Walk) == false &&
                m_owner.anim.IsType(CharacterAnimType.Walk_Back) == false)
            {
                m_owner.anim.Play(CharacterAnimType.Walk);
            }

            m_owner.rig.linearVelocity = _velocity;
        }

        if (_velocity.x != 0)
            SetFlip(_velocity.x > 0);
    }

    // 기본이 왼쪽을 보는거라, 오른쪽을 보게 하려면 Flip 해줘야 한다.
    public void SetFlip(bool _isRight)
    {
        if (_isRight == m_owner.panel.localScale.x > 0 && m_tweenDash == null)
        {
            var scale = m_owner.panel.localScale;
            scale.x *= -1;
            m_owner.panel.localScale = scale;

            m_owner.talkbox.SetFlip(_isRight);
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
        while (_target != null && _target.isLive)
        {
            // 컨트롤 중일 땐 그냥 넘어가자.
            if (ControllerManager.instance.IsControll(m_owner))
            {
                yield return null;
                continue;
            }

            var lookAt = _target.transform.position - m_owner.transform.position;
            OnMoveUpdate(lookAt.normalized * m_owner.data.moveSpeed);

            if (_isAttack && m_owner.target.Contains(_target))
            {
                m_owner.anim.Play(CharacterAnimType.Idle);

                m_owner.target.SetTarget(_target);
                yield return m_owner.attack.DoAttack();
            }

            //공격을 멈췄는데 적이ㅣ 아직 있으면 따라가야하는거 아닌가?
            if (m_owner.target.Contains(_target) == false)
            {
                var nt = m_owner.target.nearestEnemy;
                if (nt != null)
                {
                    m_owner.target.SetTarget(nt);
                    _target = nt;
                }
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
        if (m_tweenDash != null)
            return;

        m_tweenDash?.Kill();
        m_tweenDash = null;

        Vector3 lookAt = Vector3.zero, target = Vector3.zero;

        if (_targetPos == Vector3.zero)
        {
            _targetPos = m_owner.rig.linearVelocity;

            lookAt = m_owner.rig.linearVelocity;
            if (lookAt == Vector3.zero)
                lookAt = m_owner.move.isFlip ? Vector3.right : Vector3.left;
        }
        else
        {
            lookAt = (_targetPos - m_owner.transform.position);
        }
        target = m_owner.transform.position + lookAt.normalized * 5;

        DateTime dt = DateTime.Now.AddSeconds(0.1f);

        if (ControllerManager.instance.isKeyboardMode)
        {
            if (m_owner.transform.position.x < CameraManager.instance.GetMousePosition().x != lookAt.x > 0)
            {

                m_owner.anim.Play(CharacterAnimType.Dash_Back);
            }
            else
                m_owner.anim.Play(CharacterAnimType.Dash);
        }
        else if (lookAt.x != 0)
        {
            m_owner.anim.Play(CharacterAnimType.Dash);
            SetFlip(lookAt.x > 0);
        }

        bool isFlipDash = lookAt.x == 0 ? isFlip : lookAt.x > 0;
        EffectWorker.instance.Dash(m_owner, isFlipDash);

        m_tweenDash = DOTween.To(() => m_owner.transform.position, _pos => m_owner.rig.MovePosition(_pos), target, 0.2f);
        await m_tweenDash.OnUpdate(
            () =>
            {
                if (DateTime.Now > dt)
                {
                    EffectWorker.instance.Dash(m_owner, isFlipDash);
                    dt = DateTime.Now.AddSeconds(10);
                }
            }).AsyncWaitForCompletion();

        m_owner.anim.Play(CharacterAnimType.Idle);

        m_tweenDash = null;
    }
}
