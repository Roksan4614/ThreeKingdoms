using UnityEngine;

public class Character_AnimEvent : MonoBehaviour, IValidatable
{
    [SerializeField]
    CharacterComponent m_owner;

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_owner = transform.parent.parent.parent?.GetComponent<CharacterComponent>();
    }
#endif

    public void EventAttackHit()
    {
        m_owner.attack.EventAttackHit();
    }
}
