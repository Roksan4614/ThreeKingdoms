using UnityEngine;

public enum CharacterStateType
{
    None = -1,

    Wait, //대기
    Moving, // 적에게 접근
    Battle, // 전투중

    Following, // 주장 따라가기

    Max,
}

public class CharacterState_
{
    public CharacterStateType stateType { get; protected set; }

    protected CharacterComponent m_owner;

    protected CharacterState_(CharacterStateType _stateType, CharacterComponent _owner)
    {
        stateType = _stateType;
        m_owner = _owner;
    }
    public virtual void Start() { }
    public virtual void Stop() { }
    public virtual void LateUpdate() { }
}
