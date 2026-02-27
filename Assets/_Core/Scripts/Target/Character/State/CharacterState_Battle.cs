using System.Collections;
using UnityEngine;

public class CharacterState_Battle : CharacterState
{
    public CharacterState_Battle(CharacterComponent _owner)
        : base(CharacterStateType.Battle, _owner) { }

    public override IEnumerator DoUpdate()
    {
        var target = GetNearestHero();

        // 반복 중에 이게 꺼지는 경우가 있음
        m_owner.element.collider.enabled = true;

        while (true)
        {
            if (target != null)
                yield return m_owner.move.DoMoveTarget(target, true);

            target = GetNearestHero();

            if (target == null && m_owner.anim.animType != CharacterAnimType.Idle)
                m_owner.anim.Play(CharacterAnimType.Idle);

            yield return null;
        }
    }

    CharacterComponent GetNearestHero()
    {
        if (m_owner.factionType == FactionType.Alliance)
            return StageManager.instance.GetNearestEnemy(m_owner.transform.position);
        else
            return TeamManager.instance.GetNearestHero(m_owner.transform.position);
    }
}
