using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Character_Worker_Attack : Character_Worker
{
    public Character_Worker_Attack(CharacterComponent _owner) : base(_owner)
    {
        m_weapon = _owner.transform.GetComponent<Character_Weapon>("Character");

        if (m_weapon == null)
        {
            m_weapon = _owner.transform.Find("Character").AddComponent<Character_Weapon>();
            m_weapon.OnManualValidate();
        }
    }

    Character_Weapon m_weapon;

    public IEnumerator DoAttack(CharacterComponent _target)
    {
        while (_target.isLive && m_owner.target.Contains(_target))
        {
            while (m_weapon.isUseSkill)
                yield return null;

            m_weapon.Attack(UnityEngine.Random.Range(0, 100) > 50f);

            yield return new WaitForSeconds(m_owner.data.attackSpeed);
        }
    }

    public void ControlAttack()
    {
        m_owner.anim.Play(CharacterAnimType.Attack, 1);
        m_weapon.ShowSlashEffect();
    }

    public void EventAttackHit()
    {
        m_weapon.EventAttackHit(m_owner);
    }

    public bool IsValidUseSkill()
        => m_owner.isLive && m_weapon.IsValidUseSkill();

    public IEnumerator DoUseSkill()
    {
        yield return m_weapon.DoUseSkill();
    }

    public void ResetFX()
        => m_weapon.ResetFX();
}