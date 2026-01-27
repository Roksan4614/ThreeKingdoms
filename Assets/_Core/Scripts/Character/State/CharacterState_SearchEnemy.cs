using System.Collections;
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
        while (true)
        {

            yield return null;
        }
    }
}
