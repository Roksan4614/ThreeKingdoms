using System.Collections;
using System.Linq;
using UnityEngine;

public class CharacterState_SearchEnemy : CharacterState_
{
    public CharacterState_SearchEnemy(CharacterComponent _owner)
        : base(CharacterStateType.SearchEnemy, _owner) { }

    Coroutine m_coSearching;

    public override void Start()
    {
        m_coSearching = m_owner.StartCoroutine(DoSearching());
    }
    public override void Stop()
    {
        m_owner.StopCoroutine(m_coSearching);
    }

    IEnumerator DoSearching()
    {
        var mainHero = TeamManager.instance.mainHero;
        var nearestEnemy = StageManager.instance.nearestEnemy;

        var teamSpeed = TeamManager.instance.teamMoveSpeed;

        while (Input.GetKey(KeyCode.Space) == false)
            yield return null;

        m_owner.anim.PlayAnimation(CharacterAnimType.Walk);

        while (true)
        {
            var lookAt = nearestEnemy.transform.position - mainHero.transform.position;

            m_owner.rig.linearVelocity = lookAt.normalized * teamSpeed;

            if (m_owner.isMain)
            {
                var distance = Vector3.Distance(m_owner.transform.position, nearestEnemy.transform.position);

                if (distance < 5f)
                {
                    TeamManager.instance.SetState(CharacterStateType.Battle);
                    break;
                }
            }

            yield return null;
        }
    }
}
