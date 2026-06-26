using Cysharp.Threading.Tasks;
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
        if (m_owner.factionType == FactionType.Enemy)
            base.Start(_data);
    }

    public override async UniTask UpdateAsync()
    {
        // 캐릭터 재 정렬할 시간이 필요해.
        await UniTask.WaitForSeconds(.5f, cancellationToken: m_cts.Token);

        while (true)
        {
            var nearestHero = TeamManager.instance.GetNearestHero(m_owner.position);

            if (nearestHero != null)
            {
                var distance = (m_owner.position - nearestHero.transform.position).sqrMagnitude;

                if (distance < 36f)
                {
                    TeamManager.instance.SetState(CharacterStateType.Battle);
                    StageManager.instance.SetState(CharacterStateType.Battle);
                    break;
                }
            }

            await UniTask.NextFrame(cancellationToken: m_cts.Token);
        }
    }
}
