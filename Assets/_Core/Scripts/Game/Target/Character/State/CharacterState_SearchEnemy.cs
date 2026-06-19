using Cysharp.Threading.Tasks;
using System.Collections;
using System.Linq;
using UnityEngine;

public class CharacterState_SearchEnemy : CharacterState
{
    public CharacterState_SearchEnemy(CharacterComponent _owner)
        : base(CharacterStateType.SearchEnemy, _owner) { }

    public override async UniTask UpdateAsync()
    {
        var mainHero = TeamManager.instance.mainHero;
        var posTarget = StageManager.instance.centerPosition;

        var nearestEnemy = StageManager.instance.GetNearestEnemy(mainHero.transform.position);

        while (nearestEnemy == null)
        {
            nearestEnemy = StageManager.instance.GetNearestEnemy(mainHero.transform.position);
            await UniTask.Yield(cancellationToken: m_cts.Token);
        }

        var posNearestEnemy = nearestEnemy.transform.position;
        var teamSpeed = TeamManager.instance.teamMoveSpeed;

        while (true)
        {
            var lookAt = posTarget - mainHero.transform.position;
            m_owner.move.OnMoveUpdate(lookAt.normalized * teamSpeed);

            var distance = (m_owner.transform.position - posNearestEnemy).sqrMagnitude;
            if (distance < 25f)
            {
                StageManager.instance.SetState(CharacterStateType.Battle);
                TeamManager.instance.SetState(CharacterStateType.Battle);
                break;
            }

            await UniTask.Yield(cancellationToken: m_cts.Token);
        }
    }
}
