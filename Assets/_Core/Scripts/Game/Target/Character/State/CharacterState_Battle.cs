using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;

public class CharacterState_Battle : CharacterState
{
    public CharacterState_Battle(CharacterComponent _owner)
        : base(CharacterStateType.Battle, _owner) { }

    public override async UniTask UpdateAsync()
    {
        var target = GetNearestHero();
        var token = m_cts.Token;

        // 반복 중에 이게 꺼지는 경우가 있음
        m_owner.element.collider.enabled = true;

        while (true)
        {
            if (target != null)
                await m_owner.move.MoveTargetAsync(target, true).SuppressCancellationThrow();

            await UniTask.WaitUntil(() => m_owner.target.target != target, cancellationToken: token);
            await UniTask.WaitUntil(() =>
            {
                if (m_owner == null || m_owner.anim == null)
                {

                }
                return m_owner.anim.IsType(CharacterAnimType.Attack) == false;
            }
            , cancellationToken: token);

            target = GetNearestHero();

            await UniTask.Yield(cancellationToken: token);
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
