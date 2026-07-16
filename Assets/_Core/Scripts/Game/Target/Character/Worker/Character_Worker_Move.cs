using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

public class Character_Worker_Move : Character_Worker
{
    public Character_Worker_Move(CharacterComponent _owner) : base(_owner)
    {
    }

    public bool isDash => m_tweenDash != null;

    public bool isMoving => m_owner.rig.linearVelocity != Vector2.zero;
    public bool isFlip => m_owner.panel.localScale.x < 0; // 오른쪽을 보는게 플립임. 기본이 왼쪽보니까

    public void MoveStop()
    {
        m_owner.rig.linearVelocity = Vector2.zero;
        m_owner.anim.Play(CharacterAnimType.Idle);

        m_owner.target.SetTarget(null);
        m_ctsMoveTarget = m_ctsMoveTarget.ReleaseCTS();
    }

    public void OnMoveUpdate(Vector2 _velocity, bool _isAnim = true, bool _isFreezeRot = false)
    {
        if (_velocity == Vector2.zero)
            return;

        if (m_tweenDash == null)
        {
            if (m_owner.buff.IsActive(BuffType.DEBUFF_NO_MOVE))
                return;

            if (_isAnim == true &&
            m_owner.anim.IsType(CharacterAnimType.Walk) == false &&
            m_owner.anim.IsType(CharacterAnimType.Walk_Back) == false)
            {
                m_owner.anim.Play(CharacterAnimType.Walk);
            }

            m_owner.rig.linearVelocity = _velocity;
        }

        if (_velocity.x != 0 && _isFreezeRot == false)
            SetFlip(_velocity.x > 0);
    }

    public void LookTarget(Transform _target)
        => SetFlip(m_owner.transform.position.x < _target.position.x);
    public void LookTarget(MonoBehaviour _target)
        => SetFlip(m_owner.transform.position.x < _target.transform.position.x);
    public void LookTarget(Vector3 _targetPos)
        => SetFlip(m_owner.transform.position.x < _targetPos.x);

    // 기본이 왼쪽을 보는거라, 오른쪽을 보게 하려면 Flip 해줘야 한다.
    public void SetFlip(bool _isRight)
    {
        if (_isRight == m_owner.panel.localScale.x > 0 && m_tweenDash == null)
        {
            var scale = m_owner.panel.localScale;
            scale.x *= -1;
            m_owner.panel.localScale = scale;

            m_owner.talkbox.SetFlip(_isRight);

            if (m_owner.element.mount != null)
                m_owner.element.mount.localScale = scale;
        }
    }

    public void MoveToPoint(Vector3 _targetPos, bool _isFreezeRot = true)
        => MoveToPointAsync(_targetPos, _isFreezeRot).Forget();

    public async UniTask MoveToPointAsync(Vector3 _targetPos, bool _isFreezeRot = true)
    {
        m_ctsMoveTarget = m_ctsMoveTarget.ReleaseCTS(true);
        var token = m_ctsMoveTarget.Token;

        var prevFlip = isFlip;

        if (_isFreezeRot == true)
            SetFlip(m_owner.position.x < _targetPos.x);

        while (true)
        {
            var lookAt = _targetPos - m_owner.position;

            if (lookAt.sqrMagnitude < 0.0001f)
                break;

            OnMoveUpdate(lookAt.normalized * m_owner.stat.moveSpeed, _isFreezeRot: _isFreezeRot);

            await UniTask.NextFrame(cancellationToken: token);
        }
        MoveStop();
        m_owner.position = _targetPos;
    }

    public void MoveTarget(CharacterComponent _target, bool _isAttack)
    {
        MoveTargetAsync(_target, _isAttack).Forget();
    }

    CancellationTokenSource m_ctsMoveTarget;
    public async UniTask MoveTargetAsync(CharacterComponent _target, bool _isAttack)
    {
        m_ctsMoveTarget = m_ctsMoveTarget.ReleaseCTS(true);
        var token = m_ctsMoveTarget.Token;

        if (m_owner.buff.IsActive(BuffType.DEBUFF_NO_MOVE))
            return;

        while (_target != null && _target.isLive)
        {
            // 컨트롤 중일 땐 그냥 넘어가자.
            if (ControllerManager.instance.IsControll(m_owner))
            {
                await UniTask.NextFrame(cancellationToken: token);
                continue;
            }

            var lookAt = _target.transform.position - m_owner.position;
            OnMoveUpdate(lookAt.normalized * m_owner.stat.moveSpeed);

            if (_isAttack && m_owner.target.Contains(_target))
            {
                m_owner.anim.Play(CharacterAnimType.Idle);

                m_owner.target.SetTarget(_target);
                m_owner.rig.linearVelocity = Vector2.zero;

                await m_owner.attack.AttackAsync(token);
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
                //else if (_target != null)
                //    m_owner.target.SetTarget(_target);
            }

            await UniTask.NextFrame(cancellationToken: token);
        }
    }

    Tween m_tweenDash;
    public void Dash(Vector3 _targetPos)
        => DashAsync(_targetPos).Forget();
    public async UniTask DashAsync(Vector3 _targetPos, float _power = 5)
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
            lookAt = (_targetPos - m_owner.position);
        }
        target = m_owner.position + lookAt.normalized * _power;

        DateTime dt = DateTime.Now.AddSeconds(0.1f);

        var hashBuff = m_owner.buff.Add(BuffType.DEBUFF_NO_MOVE);

        if (ControllerManager.instance.isKeyboardMode)
        {
            //if (lookAt.x != 0 && m_owner.factionType == FactionType.Alliance && m_owner.position.x < CameraManager.posPointer.x != lookAt.x > 0)
            if (lookAt.x != 0 && m_owner.position.x < CameraManager.posPointer.x != lookAt.x > 0)
                m_owner.anim.Play(CharacterAnimType.Dash_Back);
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

        m_tweenDash = DOTween.To(() => m_owner.position, _pos => m_owner.rig.MovePosition(_pos), target, 0.2f).SetUpdate(UpdateType.Fixed);
        await m_tweenDash.OnUpdate(
            () =>
            {
                if (DateTime.Now > dt)
                {
                    EffectWorker.instance.Dash(m_owner, isFlipDash);
                    dt = DateTime.Now.AddSeconds(10);
                }
            }).AsyncWaitForCompletion();

        m_owner.buff.Remove(BuffType.DEBUFF_NO_MOVE, hashBuff);
        m_owner.anim.Play(CharacterAnimType.Idle);

        m_tweenDash = null;
    }
}
