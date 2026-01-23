using UnityEngine;

public abstract class Character_Worker
{
    protected readonly CharacterComponent m_owner;

    public Character_Worker(CharacterComponent _owner)
    {
        m_owner = _owner;
    }

    public virtual void OnUpdate() { }
}
