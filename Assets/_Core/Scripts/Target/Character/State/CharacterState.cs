using System.Collections;
using UnityEngine;

public enum CharacterStateType
{
    None = -1,

    Wait, //대기
    SearchEnemy, // 적에게 접근
    Battle, // 전투중

    Following, // 주장 따라가기

    Max,
}

public class CharacterState
{
    public CharacterStateType stateType { get; protected set; }

    protected CharacterComponent m_owner;
    protected CharacterState(CharacterStateType _stateType, CharacterComponent _owner)
    {
        stateType = _stateType;
        m_owner = _owner;
    }

    protected Coroutine m_coUpdate;

    public virtual void Start()
    {
        if (m_coUpdate != null)
            m_owner.StopCoroutine(m_coUpdate);
        m_coUpdate = m_owner.StartCoroutine(DoUpdate());
    }
    public virtual void Stop() {

        if (m_coUpdate != null)
            m_owner.StopCoroutine(m_coUpdate);
        m_coUpdate = null;
    }

    public virtual IEnumerator DoUpdate()
    {
        yield return null;
    }
}
