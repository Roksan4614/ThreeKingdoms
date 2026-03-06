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

    public override IEnumerator DoUpdate()
    {
        // 캐릭터 재 정렬할 시간이 필요해.
        yield return new WaitForSeconds(.5f);

        while (true)
        {
            var nearestHero = TeamManager.instance.GetNearestHero(m_owner.transform.position);

            if (nearestHero != null)
            {
                var distance = (m_owner.transform.position - nearestHero.transform.position).sqrMagnitude;

                if (distance < 36f)
                {
                    TeamManager.instance.SetState(CharacterStateType.Battle);
                    StageManager.instance.SetState(CharacterStateType.Battle);
                    break;
                }
            }
            yield return null;
        }
    }
}
