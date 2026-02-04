using System;
using System.Collections;
using UnityEngine;

public class Weapon_Guanyu : Character_Weapon
{
    private void Start()
    {
        m_durationSkill = 10f;
        m_dtOpenSkill.AddSeconds(m_durationSkill);
    }

    Coroutine m_coEndSkill;
    public override void UseSkill(bool _isForce = false)
    {
        if (DateTime.Now < m_dtOpenSkill && _isForce == false)
            return;

        m_animSkill.Play();

        m_dtOpenSkill = DateTime.Now.AddSeconds(m_durationSkill);

        isUseSkill = true;
        m_owner.anim.Play(CharacterAnimType.Attack);

        if (m_coEndSkill != null)
            StopCoroutine(m_coEndSkill);

        m_coEndSkill = StartCoroutine(Utils.DoAfterCoroutine(() =>
        {
            isUseSkill = false;
            m_coEndSkill = null;
        }, 0.5f));
    }
}
