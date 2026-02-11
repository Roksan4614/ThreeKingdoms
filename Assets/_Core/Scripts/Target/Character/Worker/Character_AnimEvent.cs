using UnityEngine;

public class Character_AnimEvent : MonoBehaviour
{
    [SerializeField]
    CharacterComponent m_owner;

#if UNITY_EDITOR
    private void OnValidate()
    {
        m_owner = transform.parent.parent.parent?.GetComponent<CharacterComponent>();

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    public void EventAttackHit()
    {
        m_owner.attack.EventAttackHit();
    }
}
