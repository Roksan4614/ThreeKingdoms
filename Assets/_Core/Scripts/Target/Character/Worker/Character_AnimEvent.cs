using UnityEngine;

public class Character_AnimEvent : MonoBehaviour
{
    CharacterComponent m_owner;

    private void Awake()
    {
        m_owner = transform.parent.parent.parent.GetComponent<CharacterComponent>();
    }

    public void EventAttack()
    {
        m_owner.attack.EventAttack();
    }
}
