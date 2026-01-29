using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Worker_Attack : Character_Worker
{
    public Character_Worker_Attack(CharacterComponent _owner) : base(_owner)
    {
    }

    public IEnumerator DoAttack(CharacterComponent _target)
    {
        while (_target.isLive && m_owner.target.Contains(_target))
        {
            m_owner.anim.Play(CharacterAnimType.Attack);

            yield return new WaitForSeconds(m_owner.data.attackSpeed);
        }
    }

    public void EventAttack()
    {
        var target = m_owner.target.target;
        if (target == null || target.isLive == false)
            return;

        if (target.OnDamage(m_owner.data.attackPower))
            m_owner.target.SetTarget(null);
    }
}