using System.Collections;
using UnityEngine;

public class CharacterState_Battle : CharacterState
{
    public CharacterState_Battle(CharacterComponent _owner)
        : base(CharacterStateType.Battle, _owner) { }

    public override IEnumerator DoUpdate()
    {
        var target = StageManager.instance.GetNearestHero(m_owner.transform.position);

        while (target != null)
        {
            yield return m_owner.move.DoMoveTarget(target, true);

            while (target.isLive == true)
                yield return null;

            target = StageManager.instance.GetNearestHero(m_owner.transform.position);
        }

        m_owner.anim.Play(CharacterAnimType.Idle);
    }
}
