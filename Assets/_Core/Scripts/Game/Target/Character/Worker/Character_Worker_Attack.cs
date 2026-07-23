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

    public async UniTask AttackAsync(CancellationToken _token)
    {
        while (true)
        {
            await UniTask.WaitUntil(() => m_owner.move.isDash == false);

            if (m_owner.target.isAttackTarget)
            {
                if (m_ctsAttackPush == null &&
                    m_timeAttack < Time.realtimeSinceStartup &&
                    m_weapon.isUseSkill == false)
                {
                    m_weapon.Attack(IsCritical());
                    m_timeAttack = Time.realtimeSinceStartup + m_owner.stat.attackSpeed;
                }
            }
            else if (m_timeAttack < Time.realtimeSinceStartup)
                break;

            await UniTask.NextFrame(cancellationToken: _token);
        }
    }

    CancellationTokenSource m_ctsAttackPush;


    public async UniTask ControlAttackAsync(UnityAction _onAttack, bool _isPushButton)
    {
        if (m_owner.move.isDash)
            return;

        m_ctsAttackPush = m_ctsAttackPush.ReleaseCTS(true);
        var token = m_ctsAttackPush.Token;

        if (m_timeAttack - m_owner.stat.attackSpeed * 0.5f > Time.realtimeSinceStartup)
        {
            while (m_timeAttack > Time.realtimeSinceStartup)
                await UniTask.NextFrame(token, true);
            _isPushButton = false;
        }

        m_timeAttack = -1;

        while ((ControllerManager.instance.isKeyboardMode && ControllerManager.instance.isLeftClick == true) ||
            Input.GetKey(KeyCode.X) ||
            _isPushButton == true)
        {
            if (m_timeAttack < Time.realtimeSinceStartup && m_weapon.isUseSkill == false)
            {
                m_owner.target.SetTargetNearest();

                _onAttack();

                bool isCritical = m_owner.target.target != null && IsCritical();
                if (isCritical == false || m_timeAttack == -1)
                    ShowSlashEffect(true);

                m_weapon.Attack(isCritical);

                m_timeAttack = Time.realtimeSinceStartup + m_owner.stat.attackSpeed;
            }
            _isPushButton = false;
            await UniTask.NextFrame(token, true);
        }

        m_ctsAttackPush = null;
    }

    public async UniTask RushAsync(Vector3 _targetPos, bool _isCameraShake = true)
    {
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

                    m_owner.anim.AttackMotionEnd();
                    m_owner.attack.ShowSlashEffect(true);
                }
            });
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

    public void EventAttackEnd()
    {
    }

    public bool IsValidUseSkill()
        => m_owner.isLive && m_weapon.IsValidUseSkill();

    public bool isUseSkill { get; private set; }

    public async UniTask UseSkillAsync()
    {
        m_timeAttack = Time.realtimeSinceStartup + m_owner.stat.attackSpeed;
        isUseSkill = true;
        await m_weapon.UseSkillAsync();
    }

    public void ShowSlashEffect(bool _isShake = false) => m_weapon.ShowSlashEffect(_isForceShake: _isShake);

    public void ResetFX()
    {
        m_weapon.ResetFX();
        isRunningAttack = false;
    }

    public void Die()
    {
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