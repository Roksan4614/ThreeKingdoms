using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

    float m_timeAttack;

    public IEnumerator DoAttack()
    {
        while (true)
        {
            if (m_owner.target.isAttackTarget)
            {
                if (m_timeAttack < Time.realtimeSinceStartup && m_weapon.isUseSkill == false)
                {
                    m_weapon.Attack(IsCritical());
                    m_timeAttack = Time.realtimeSinceStartup + m_owner.data.attackSpeed;
                }
            }
            else if (m_timeAttack < Time.realtimeSinceStartup)
                break;
            yield return null;
        }
    }

    public void ControlAttack()
    {
        m_owner.target.SetTargetNearest();

        bool isCritical = m_owner.target.target != null && IsCritical();
        m_weapon.Attack(isCritical, 1);

        if (isCritical == false)
            m_weapon.ShowSlashEffect(_isForceShake: m_owner.target.target != null);

        if (m_owner.target.isAttackTarget)
            m_timeAttack = Time.realtimeSinceStartup + m_owner.data.attackSpeed;
    }

    bool IsCritical()
    {
        bool isCritical = UnityEngine.Random.Range(0, 100) > 50f;
        return isCritical;
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