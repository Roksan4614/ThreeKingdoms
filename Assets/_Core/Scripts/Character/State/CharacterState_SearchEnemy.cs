using System.Collections;
using UnityEngine;

public class CharacterState_SearchEnemy : CharacterState_
{
    public CharacterState_SearchEnemy(CharacterComponent _owner)
        : base(CharacterStateType.SearchEnemy, _owner) { }

    public override void Start()
    {
    }
}
