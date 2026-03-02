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
        while (m_owner.target.isAttackTarget)
        {
            while (m_weapon.isUseSkill)
                yield return null;

            m_weapon.Attack(IsCritical());

            m_timeAttack = Time.realtimeSinceStartup + m_owner.data.attackSpeed;
            while (m_timeAttack > Time.realtimeSinceStartup)
                yield return null;
        }
    }

    public void ControlAttack()
    {
        if (Time.realtimeSinceStartup < m_timeAttack)
            return;

        m_owner.target.SetTargetNearest();

        bool isCritical = IsCritical();
        m_weapon.Attack(isCritical, 1);

        if (isCritical == false)
            m_weapon.ShowSlashEffect(_isForceShake: m_owner.target.target != null);

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