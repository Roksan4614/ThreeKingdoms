using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class Character_Weapon : MonoBehaviour, IValidatable
{
    [SerializeField]
    protected CharacterComponent m_owner;
    [SerializeField]
    protected List<SpriteAnimaion> m_animSlash = new();
    [SerializeField]
    protected Transform m_skillRange;

    protected bool m_isCritial;
    public bool isUseSkill { get; protected set; }
    protected bool isControllSkillPosition { get; private set; }

    protected CancellationTokenSource m_cts;

    protected virtual void Awake()
    {
        for (int i = 0; i < m_animSlash.Count; i++)
            m_animSlash[i].gameObject.SetActive(false);

        if (m_skillRange != null)
            m_skillRange.gameObject.SetActive(false);
        else
        {

        }
    }

    protected virtual void OnDestroy()
    {
        if (m_animSlash.Count > 0 && m_animSlash[0].transform.parent != m_owner.panel)
        {
            for (int i = 0; i < m_animSlash.Count; i++)
            {
                Destroy(m_animSlash[i].gameObject);
            }
        }

        ReleaseCTS();
    }

    public void ReleaseCTS()
        => m_cts = m_cts.Release();

    public bool isAttack { get; private set; } = false;
    public void Attack(bool _isCritical)
    {
        isAttack = true;

        m_isCritial = _isCritical;
        if (m_isCritial)
            ShowSlashEffect(
                _isForceShake: m_owner.isMain == true || ControllerManager.instance.isDoing == false);

        if (m_owner.move.isMoving)
            m_owner.anim.Play(CharacterAnimType.Attack_Move, 1);
        else
            m_owner.anim.Play(CharacterAnimType.Attack, 0);
    }

    public virtual bool IsValidUseSkill() =>
        m_owner.target.target != null &&
        m_owner.target.target.isLive &&
        m_owner.buff.IsActive(BuffType.DEBUFF_NO_SKILL) == false;

    public virtual async UniTask UseSkillAsync()
    {
        m_isCritial = false;
        isUseSkill = true;

        m_owner.anim.Play(CharacterAnimType.Attack);
        ShowSlashEffect(true);

        CameraManager.instance.Shake();

        await UniTask.Yield();

        isUseSkill = false;
    }

    public virtual void EventAttackHit(CharacterComponent _owner)
    {
        isAttack = false;

        if (isUseSkill == true || _owner.target.isAttackTarget == false)
            return;

        var target = _owner.target.target;
        if (target == null || target.isLive == false)
            return;

        var damage = _owner.stat.attackPower;
        if (m_isCritial == true)
            damage = (int)(damage * _owner.stat.criticalDamage);

        if (target.OnDamage(_owner, damage))
            _owner.target.SetTarget(null);
    }

    public void ShowSlashEffect(bool _isWorld = false, bool _isForceShake = false)
    {
        if (m_animSlash.Count == 0)
            return;

        CameraManager.instance.Shake(_isForceShake);

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

    public virtual void Die()
    {
    }

    public bool isRunningSlash
    {
        get
        {
            for (int i = 0; i < m_animSlash.Count; i++)
            {
                if (m_animSlash[i].gameObject.activeSelf == true)
                    return true;
            }
            return false;
        }
    }

    public virtual void OnDrag_ControllSkill(Vector3 _targetPos) { }
    public virtual void OnUp_ControllSkill() { }
    public virtual void OnCancel_ControllSkill() { }

    public virtual void OnManualValidate()
    {
        m_owner = transform.parent?.GetComponent<CharacterComponent>();
        m_skillRange = m_owner.transform.Find("SkillRange");

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

}
