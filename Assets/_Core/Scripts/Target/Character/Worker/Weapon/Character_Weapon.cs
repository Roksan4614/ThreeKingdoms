using System;
using System.Collections;
using UnityEngine;

public class Character_Weapon : MonoBehaviour
{
    protected SpriteAnimaion m_animSkill;
    protected CharacterComponent m_owner;

    protected float m_durationSkill;
    protected DateTime m_dtOpenSkill;

    public bool isUseSkill { get; protected set; }

    private void Awake()
    {
        m_owner = transform.parent.GetComponent<CharacterComponent>();

        m_animSkill = transform.GetComponent<SpriteAnimaion>("Panel/FxAttack");
        m_animSkill.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && m_animSkill.transform.childCount > 0)
            UseSkill(true);
    }

    Coroutine m_coEndSkill;
    public virtual void UseSkill(bool _isForce = false)
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

    public virtual void EventAttackHit(CharacterComponent _owner)
    {
        if (isUseSkill == true)
            return;

        var target = _owner.target.target;
        if (target == null || target.isLive == false)
            return;

        var damage = _owner.data.attackPower;

        bool isCritical = UnityEngine.Random.Range(0, 100) < 30;

        EffectWorker.instance.SlotDamageTakenEffect(new()
        {
            attacker = _owner.transform,
            target = target.transform,
            value = -damage,
            isCritical = isCritical,
            isAlliance = target.factionType == FactionType.Alliance
        });

        if (isCritical)
            m_animSkill.Play();

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
