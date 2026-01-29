using System.Collections;
using UnityEngine;

public class CharacterState_Wait : CharacterState
{
    public CharacterState_Wait(CharacterComponent _owner)
        : base(CharacterStateType.Wait, _owner) { }

    public override void Start()
    {
        m_owner.move.MoveStop();
        m_owner.anim.Play(CharacterAnimType.Idle);
    }
}
