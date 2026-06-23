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
            // 타겟이 없으면 검색이 될 때까지 찾는다.
            while (target == null)
            {
                target = GetNearestHero();
                await UniTask.Yield(cancellationToken: token);
            }

            // 적에게 움직이면서 공격한다.
            if (target != null)
                await m_owner.move.MoveTargetAsync(target, true).SuppressCancellationThrow();

            // 움직임을 멈출수도 있다.
            await UniTask.WaitUntil(() => m_owner.buff.IsActive(BuffType.DEBUFF_NO_MOVE) == false);

            // 타겟이 달라졌는지, 타겟이 없는지, 타겟이 죽었는지를 체크한다.
            await UniTask.WaitUntil(() => m_owner.target.target != target || target == null || target.isLive == false, cancellationToken: token);

            // 공격 모션이면 기다려주자
            await UniTask.WaitUntil(() => m_owner.anim.IsType(CharacterAnimType.Attack) == false, cancellationToken: token);

            // 대기 모션이 아니면 대기로 해주기.
            if (m_owner.anim.IsType(CharacterAnimType.Idle) == false)
                m_owner.anim.Play(CharacterAnimType.Idle);

            target = GetNearestHero();
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
