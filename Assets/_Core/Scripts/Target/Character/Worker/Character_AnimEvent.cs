using UnityEngine;

public class Character_AnimEvent : MonoBehaviour
{
    CharacterComponent m_owner;

    private void Awake()
    {
        m_owner = transform.parent.parent.parent.GetComponent<CharacterComponent>();
    }

    public void EventAttackHit()
    {
        m_owner.attack.EventAttackHit();
    }
}
