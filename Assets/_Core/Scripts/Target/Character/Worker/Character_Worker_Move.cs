using Mono.Cecil.Cil;
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
    }

    public void OnMoveUpdate(Vector2 _velocity)
    {
        if (_velocity == Vector2.zero)
            return;

        if (m_owner.anim.animType != CharacterAnimType.Walk)
            m_owner.anim.Play(CharacterAnimType.Walk);

        m_owner.rig.linearVelocity = _velocity;

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
                yield return m_owner.attack.DoAttack(_target);
            }

            yield return null;
        }

        m_coMoveTarget = null;
    }
}
