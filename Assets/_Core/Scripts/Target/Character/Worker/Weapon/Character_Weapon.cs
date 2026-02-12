using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Weapon : MonoBehaviour, IValidatable
{
    [SerializeField]
    public CharacterComponent m_owner;
    [SerializeField]
    public List<SpriteAnimaion> m_animSlash = new();

    bool m_isCritial;
    public bool isUseSkill { get; protected set; }

    private void Awake()
    {
        for (int i = 0; i < m_animSlash.Count; i++)
            m_animSlash[i].gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_owner = transform.parent?.GetComponent<CharacterComponent>();

        m_animSlash.Clear();
        var fxAttack = transform.GetComponent<SpriteAnimaion>("Panel/FxAttack");
        if (fxAttack != null && fxAttack.transform.childCount > 0)
        {
            m_animSlash.Add(fxAttack);

            for (int i = 1; i < fxAttack.transform.childCount; i++)
            {
                var sub = fxAttack.transform.GetChild(i).GetComponent<SpriteAnimaion>();
                if (sub != null)
                    m_animSlash.Add(sub);
            }
        }
    }
#endif

    public void Attack(bool _isCritical)
    {
        m_isCritial = _isCritical;

        if (m_isCritial)
            ShowSlashEffect();

        m_owner.anim.Play(CharacterAnimType.Attack);
    }

    public virtual bool IsValidUseSkill() => m_owner.target.target != null;

    public virtual IEnumerator DoUseSkill()
    {
        m_isCritial = false;
        isUseSkill = true;

        m_owner.anim.Play(CharacterAnimType.Attack);
        ShowSlashEffect();

        yield return null;

        isUseSkill = false;
    }

    public virtual void EventAttackHit(CharacterComponent _owner)
    {
        if (isUseSkill == true)
            return;

        var target = _owner.target.target;
        if (target == null || target.isLive == false)
            return;

        var damage = _owner.data.attackPower;

        EffectWorker.instance.SlotDamageTakenEffect(new()
        {
            attacker = _owner.transform,
            target = target,
            value = -damage,
            isCritical = m_isCritial,
            isAlliance = target.factionType == FactionType.Alliance
        });
        m_isCritial = false;

        if (target.OnDamage(damage))
            _owner.target.SetTarget(null);
    }

    public void ShowSlashEffect()
    {
        for (int i = 0; i < m_animSlash.Count; i++)
            m_animSlash[i].Play();
    }
}
