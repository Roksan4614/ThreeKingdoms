using System.Collections;
using UnityEngine;

public class CharacterState_Battle : CharacterState
{
    public CharacterState_Battle(CharacterComponent _owner)
        : base(CharacterStateType.Battle, _owner) { }

    public override IEnumerator DoUpdate()
    {
        var target = GetNearestHero();

        while (target != null)
        {
            yield return m_owner.move.DoMoveTarget(target, true);

            target = GetNearestHero();
        }

        m_owner.anim.Play(CharacterAnimType.Idle);
    }

    CharacterComponent GetNearestHero()
    {
        if( m_owner.factionType == FactionType.Alliance)
            return StageManager.instance.GetNearestEnemy(m_owner.transform.position);
        else
            return TeamManager.instance.GetNearestHero(m_owner.transform.position);
    }
}
