using DG.Tweening;
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

    private void OnDestroy()
    {
        if (m_animSlash.Count > 0 && m_animSlash[0].transform.parent != m_owner.panel)
        {
            for (int i = 0; i < m_animSlash.Count; i++)
            {
                Destroy(m_animSlash[i].gameObject);
            }
        }
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
        ShowSlashEffect(true);

        CameraManager.instance.Shake();

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
        if (m_isCritial == true)
            damage = (int)(damage * _owner.data.criticalDamage);

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

    public void ShowSlashEffect(bool _isWorld = false)
    {
        if (m_animSlash.Count == 0)
            return;

        if (TeamManager.instance.teamState != CharacterStateType.Battle)
            return;

        CameraManager.instance.Shake();

        for (int i = 0; i < m_animSlash.Count; i++)
            m_animSlash[i].Play();

        var fxSlash = m_animSlash[0].transform;

        // 제일 위로 올리기 위한 작업
        {
            if (_isWorld == (fxSlash.parent == m_owner.panel))
            {
                if (_isWorld)
                    fxSlash.localPosition = Vector3.zero;

                fxSlash.SetParent(_isWorld ? EffectWorker.instance.element.renderer : m_owner.panel);

                if (_isWorld == false)
                    fxSlash.localPosition = Vector3.zero;
            }

            if (_isWorld == false)
            {
                if (fxSlash.localScale.x < 0)
                {
                    var scale = fxSlash.localScale;
                    scale.x *= -1;
                    fxSlash.localScale = scale;
                }
            }
            else if (m_owner.move.isFlip == fxSlash.localScale.x > 0)
            {
                var scale = fxSlash.localScale;
                scale.x *= -1;
                fxSlash.localScale = scale;
            }
        }
        // 제일 위로 올리기 위한 작업
    }

    public void ResetFX()
    {
        for (int i = 0; i < m_animSlash.Count; i++)
            m_animSlash[i].Stop();
    }
}
