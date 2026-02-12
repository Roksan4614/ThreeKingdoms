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
        var mainHero = TeamManager.instance.mainHero;

        while (true)
        {
            var distance = (m_owner.transform.position - mainHero.transform.position).sqrMagnitude;

            if (distance < 36f)
            {
                TeamManager.instance.SetState(CharacterStateType.Battle);
                StageManager.instance.SetState(CharacterStateType.Battle);
                break;
            }
            yield return null;
        }
    }
}
