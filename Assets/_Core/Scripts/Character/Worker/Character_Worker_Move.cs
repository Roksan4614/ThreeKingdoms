using UnityEngine;

public class Character_Woker_Move : Character_Worker
{
    public Character_Woker_Move(CharacterComponent _owner) : base(_owner)
    {
    }
    public bool isMoving => m_owner.rig.linearVelocity == Vector2.zero;

    public void MoveStop()
    {
        m_owner.rig.linearVelocity = Vector2.zero;
        m_owner.anim.PlayAnimation(CharacterAnimType.Idle);
    }

    public void OnMoveUpdate(Vector2 _lookAt)
    {
        if (_lookAt == Vector2.zero)
            return;

        if (m_owner.anim.playingAnimType != CharacterAnimType.Walk)
            m_owner.anim.PlayAnimation(CharacterAnimType.Walk);

        m_owner.rig.linearVelocity = _lookAt.normalized * m_owner.data.moveSpeed;

        SetFlip(_lookAt.x > 0);
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
}
