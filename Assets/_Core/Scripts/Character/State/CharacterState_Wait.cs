using System.Collections;
using UnityEngine;

public class CharacterState_Wait : CharacterState_
{
    public CharacterState_Wait(CharacterComponent _owner)
        : base(CharacterStateType.Wait, _owner) { }

    public override void Start()
    {
        m_owner.move.MoveStop();
        m_owner.anim.PlayAnimation(CharacterAnimType.Idle);
    }
}
