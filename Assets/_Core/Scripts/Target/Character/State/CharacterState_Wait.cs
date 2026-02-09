using System.Collections;
using UnityEngine;

public class CharacterState_Wait : CharacterState
{
    public CharacterState_Wait(CharacterComponent _owner)
        : base(CharacterStateType.Wait, _owner) { }

    public override void Start(params object[] _data)
    {
        m_owner.target.SetTarget(null);
        m_owner.move.MoveStop();
    }
}
