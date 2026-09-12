using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Character_Worker_Attack : Character_Worker
{
    public bool isAttack => m_weapon.isAttack;
    public bool isRunningAttack { get; set; }
    // Monotonic observations of actual actions; they do not control combat or input.
    public long controlAttackCount { get; private set; }
    public long skillUseCount { get; private set; }

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
    bool m_waitingAttackHit;
    int m_attackStartedFrame;
    int m_pendingAttackLayer;
    CharacterAnimType m_pendingAttackAnimation;

    internal static float GetAttackIntervalSeconds(float attacksPerSecond)
    {
        if (float.IsNaN(attacksPerSecond) || float.IsInfinity(attacksPerSecond) || attacksPerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(attacksPerSecond), "Attack rate must be finite and positive.");
        var interval = 1f / attacksPerSecond;
        if (float.IsInfinity(interval) || interval <= 0)
            throw new ArgumentOutOfRangeException(nameof(attacksPerSecond), "Attack interval cannot be represented.");
        return interval;
    }

    float AttackIntervalSeconds => GetAttackIntervalSeconds(m_owner.stat.attackSpeed);

    bool IsWaitingForAttackHit
    {
        get
        {
            if (!m_waitingAttackHit) return false;
            // CrossFade is evaluated later in the frame. Preserve the pending hit until that first evaluation.
            if (Time.frameCount == m_attackStartedFrame) return true;
            if (m_owner.anim.IsType(m_pendingAttackAnimation, m_pendingAttackLayer)) return true;
            // Walk, dash, knockdown or another state interrupted this attack before its event.
            m_waitingAttackHit = false;
            return false;
        }
    }

    void AwaitAttackHit(bool moving)
    {
        m_waitingAttackHit = true;
        m_attackStartedFrame = Time.frameCount;
        m_pendingAttackLayer = moving ? 1 : 0;
        m_pendingAttackAnimation = moving ? CharacterAnimType.Attack_Move : CharacterAnimType.Attack;
    }

    public async UniTask AttackAsync(CancellationToken _token)
    {
        while (true)
        {
            await UniTask.WaitUntil(() => m_owner.move.isDash == false);

            if (m_owner.target.isAttackTarget)
            {
                if (m_ctsAttackPush == null &&
                    m_timeAttack < Time.realtimeSinceStartup &&
                    m_weapon.isUseSkill == false && !isUseSkill && !IsWaitingForAttackHit)
                {
                    var interval = AttackIntervalSeconds;
                    AwaitAttackHit(m_owner.move.isMoving);
                    m_weapon.Attack(IsCritical());
                    m_timeAttack = Time.realtimeSinceStartup + interval;
                }
            }
            else if (m_timeAttack < Time.realtimeSinceStartup)
                break;

            await UniTask.NextFrame(_token);
        }
    }

    CancellationTokenSource m_ctsAttackPush;
    //public bool isAttackPush { get; private set; }
    public bool isAttackPush => m_ctsAttackPush != null;

    public async UniTask ControlAttackAsync(UnityAction _onAttack, bool _isPushButton)
    {
        if (m_owner.move.isDash)
            return;

        m_ctsAttackPush = m_ctsAttackPush.ReleaseCTS(true);
        var token = m_ctsAttackPush.Token;

        if (m_timeAttack - AttackIntervalSeconds * 0.5f > Time.realtimeSinceStartup)
        {
            while (m_timeAttack > Time.realtimeSinceStartup)
                await UniTask.NextFrame(token, true);
            _isPushButton = false;
        }

        while (IsWaitingForAttackHit)
            await UniTask.NextFrame(token, true);

        m_timeAttack = -1;

        while ((ControllerManager.instance.isKeyboardMode && ControllerManager.instance.isLeftClick == true) ||
            Input.GetKey(KeyCode.X) ||
            _isPushButton == true)
        {
            if (m_timeAttack < Time.realtimeSinceStartup && m_weapon.isUseSkill == false && !isUseSkill && !IsWaitingForAttackHit)
            {
                var interval = AttackIntervalSeconds;
                m_owner.target.SetTargetNearest();

                _onAttack();

                bool isCritical = m_owner.target.target != null && IsCritical();
                if (isCritical == false || m_timeAttack == -1)
                    ShowSlashEffect(true);

                AwaitAttackHit(m_owner.rig.linearVelocity != Vector2.zero);
                m_owner.anim.PlayAttack();
                controlAttackCount++;
                //isAttackPush = true;

                m_timeAttack = Time.realtimeSinceStartup + interval;
            }
            _isPushButton = false;
            await UniTask.NextFrame(token, true);
            //isAttackPush = false;
        }

        m_ctsAttackPush = null;
    }

    public void Rush(Vector3 _targetPos, bool _isCameraShake = true)
        => RushAsync(_targetPos, _isCameraShake).Forget();

    public bool isRush { get; private set; }
    public async UniTask RushAsync(Vector3 _targetPos, bool _isCameraShake = true)
    {
        m_waitingAttackHit = false;
        isRush = true;
        m_owner.move.MoveStop();
        m_owner.move.SetFlip(_targetPos.x > m_owner.position.x);

        DateTime dt = DateTime.Now.AddSeconds(0.1f);
        EffectWorker.instance.Dash(m_owner, m_owner.move.isFlip);

        if (_isCameraShake)
            CameraManager.instance.Shake();

        m_owner.anim.AttackMotionFirstFrame(CharacterAnimType.Attack_Move, 1);
        await DOTween.To(() => m_owner.position, _pos => m_owner.rig.MovePosition(_pos), _targetPos, 0.2f).SetUpdate(UpdateType.Fixed)
            .OnUpdate(() =>
            {
                if (DateTime.Now > dt)
                {
                    EffectWorker.instance.Dash(m_owner, m_owner.move.isFlip);
                    dt = DateTime.Now.AddSeconds(10);

                    m_owner.anim.SetSpeed(1f);
                    m_owner.attack.ShowSlashEffect(true);
                }
            });
        m_owner.anim.SetSpeed(1f);

        isRush = false;
    }

    public void ShowHitEffect(CharacterComponent _target)
        => EffectWorker.instance.ShowHitEffect(m_owner.transform, _target);

    bool IsCritical()
    {
        bool isCritical = UnityEngine.Random.Range(0, 100) > 50f;
        return isCritical;
    }

    public void EventAttackHit()
    {
        m_waitingAttackHit = false;
        m_weapon.EventAttackHit(m_owner);
    }

    public void EventAttackEnd()
    {
    }

    public bool IsValidUseSkill()
        => m_owner.isLive && m_weapon.IsValidUseSkill();

    public bool isUseSkill { get; private set; }
    public void SetType_UseSkill(bool _isUseSkill)
    {
        if (_isUseSkill) m_waitingAttackHit = false;
        isUseSkill = _isUseSkill;
    }

    public async UniTask UseSkillAsync()
    {
        m_waitingAttackHit = false;
        m_timeAttack = Time.realtimeSinceStartup + AttackIntervalSeconds;
        isUseSkill = true;
        await m_weapon.UseSkillAsync();
        skillUseCount++;
        isUseSkill = false;
    }

    public void ShowSlashEffect(bool _isShake = false) => m_weapon.ShowSlashEffect(_isForceShake: _isShake);

    public void ResetFX()
    {
        m_waitingAttackHit = false;
        m_weapon.ResetFX();
        isRunningAttack = false;
    }

    public void Die()
    {
        m_waitingAttackHit = false;
        m_ctsAttackPush = m_ctsAttackPush.ReleaseCTS();
        m_weapon.Die();
    }

    public bool isRunningSlash => m_weapon.isRunningSlash;

    public void OnDrag_ControllSkill(Vector3 _targetPos)
        => m_weapon.OnDrag_ControllSkill(_targetPos);
    public void OnUp_ControllSkill()
        => m_weapon.OnUp_ControllSkill();
    public void OnCancel_ControllSkill()
        => m_weapon.OnCancel_ControllSkill();

    public void SetActive_Weapon(bool _isActive)
        => m_weapon.SetActive_Weapon(_isActive);
}