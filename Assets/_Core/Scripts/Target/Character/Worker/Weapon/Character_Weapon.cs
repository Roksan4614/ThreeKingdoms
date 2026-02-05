using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Weapon : MonoBehaviour
{
    protected List<SpriteAnimaion> m_animSkill = new();
    protected CharacterComponent m_owner;

    protected float m_durationSkill;
    protected DateTime m_dtOpenSkill;

    public bool isUseSkill { get; protected set; }

    private void Awake()
    {
        m_owner = transform.parent.GetComponent<CharacterComponent>();

        var fxAttack = transform.GetComponent<SpriteAnimaion>("Panel/FxAttack");
        if (fxAttack.transform.childCount > 0)
        {
            m_animSkill.Add(fxAttack);

            for (int i = 1; i < fxAttack.transform.childCount; i++)
            {
                var sub = fxAttack.transform.GetChild(i).GetComponent<SpriteAnimaion>();
                if (sub != null)
                    m_animSkill.Add(sub);
            }
        }

        for (int i = 0; i < m_animSkill.Count; i++)
            m_animSkill[i].gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && m_animSkill.Count > 0)
            UseSkill(true);
    }

    Coroutine m_coEndSkill;
    public virtual bool UseSkill(bool _isForce = false)
    {
        if (DateTime.Now < m_dtOpenSkill && _isForce == false)
            return false;

        for (int i = 0; i < m_animSkill.Count; i++)
            m_animSkill[i].Play();

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

        return true;
    }

    public virtual void EventAttackHit(CharacterComponent _owner)
    {
        if (isUseSkill == true)
            return;

        var target = _owner.target.target;
        if (target == null || target.isLive == false)
            return;

        var damage = _owner.data.attackPower;

        bool isCritical =
            _owner.factionType == FactionType.Alliance && UnityEngine.Random.Range(0, 100) < 30;

        EffectWorker.instance.SlotDamageTakenEffect(new()
        {
            attacker = _owner.transform,
            target = target.transform,
            value = -damage,
            isCritical = isCritical,
            isAlliance = target.factionType == FactionType.Alliance
        });

        if (isCritical)
            for (int i = 0; i < m_animSkill.Count; i++)
                m_animSkill[i].Play();

        if (target.OnDamage(damage))
            _owner.target.SetTarget(null);
    }

    IEnumerator DoAutoUseSKill()
    {
        bool isAuto = false;

        while (true)
        {
            yield return null;

            while (isAuto == true)
                continue;
        }
    }
}
