using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Character_Worker_Attack : Character_Worker
{
    public Character_Worker_Attack(CharacterComponent _owner) : base(_owner)
    {
        m_weapon = _owner.transform.GetComponent<Character_Weapon>("Character")
            ?? _owner.transform.Find("Character").AddComponent<Character_Weapon>();
    }

    Character_Weapon m_weapon;

    public IEnumerator DoAttack(CharacterComponent _target)
    {
        while (_target.isLive && m_owner.target.Contains(_target))
        {
            yield return m_weapon.DoAttack(m_owner);

            m_owner.anim.Play(CharacterAnimType.Attack);

            yield return new WaitForSeconds(m_owner.data.attackSpeed);
        }
    }

    public void EventAttackHit()
    {
        m_weapon.EventAttackHit(m_owner);
    }
}