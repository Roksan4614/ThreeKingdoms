using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Data_BossRaid;

public class Weapon_Vanguard_Lubu_BossRaid : Weapon_Vanguard_Lubu
{
    enum BossRaidSkillType_LuBu
    {
        NONE,

        Swing,
        Jump
    }

    BossRaidSkillType_LuBu m_skillType;

    [SerializeField] AnimationCurve m_curveJump;
    [SerializeField] int m_waitJumpFrame = 10;
    [SerializeField] float m_durationJump = .5f;

    protected override void Awake()
    {
        base.Awake();

        if (m_element.animSkillJump)
            m_element.animSkillJump.gameObject.SetActive(true);

        if (BossRaidWorker.instance.isRunning)
        {
            Signal.instance.BossRaidStatus.connect = SlotBossRaidStatus;
            SkillAsync().Forget();
        }
    }

    void SlotBossRaidStatus(BossRaidStatusType _statusType)
    {
        switch (_statusType)
        {
            case BossRaidStatusType.FirstPhase:
            case BossRaidStatusType.SecondPhase:
                SkillAsync().Forget();
                break;
            case BossRaidStatusType.Finished:
                break;
            default:
                m_cts = m_cts.Release();
                break;
        }
    }

    async UniTask SkillAsync()
    {
        m_cts = m_cts.Release(true);
        var token = m_cts.Token;

        await UniTask.NextFrame(cancellationToken: token);
        //test
        TeamManager.instance.mainHero.buff.Remove(-1, BuffType.BUFF_NO_TAKEN_DAMAGE);
        TeamManager.instance.mainHero.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);
        m_owner.buff.Remove(-1, BuffType.BUFF_NO_TAKEN_DAMAGE);
        m_owner.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);

        while (true)
        {
            m_skillType = BossRaidSkillType_LuBu.NONE;
            await UniTask.WaitForSeconds(3f, cancellationToken: token);

            await SkillAsync_Jump();

            //await UniTask.WaitForSeconds(3f, cancellationToken: token);

            //await SkillAsync_Swing();
        }
    }

    float m_speedCircleMove = 20;
    async UniTask SkillAsync_Jump()
    {
        var token = m_cts.Token;

        m_skillType = BossRaidSkillType_LuBu.Jump;

        m_owner.move.MoveStop();
        var hashDebuff = m_owner.buff.Add(BuffType.DEBUFF_NO_MOVE);

        // 차징시작
        m_owner.anim.Play("Boss_Skill_Jin");

        // 루프중이야?
        await UniTask.WaitUntil(() =>
        {
            m_owner.move.SetFlip(TeamManager.instance.GetFarthestHero(m_owner.transform.position).transform.position.x > m_owner.transform.position.x);
            return m_owner.anim.IsType("Boss_Skill_Jin_Charge");
        }, cancellationToken: token);

        // 그럼 영역생성
        m_element.warning_Circle.transform.localScale = new Vector3(2.68f, 2.68f, 1);
        m_element.warning_Circle.ShowAsync(1, token, false).Forget();

        // 영역이 영웅을 따라다녀야 해
        {
            var endTime = Time.time + 1;

            // 뒤에 장수가 있어?
            var target = TeamManager.instance.GetFarthestHero(m_owner.transform.position);
            m_element.warning_Circle.transform.position = m_owner.transform.position;

            while (Time.time < endTime)
            {
                Vector3 currentPos = m_element.warning_Circle.transform.position;
                Vector3 targetPos = target.transform.position;

                m_element.warning_Circle.transform.position = Vector3.MoveTowards(currentPos, targetPos, m_speedCircleMove * Time.deltaTime);

                m_owner.move.SetFlip(m_element.warning_Circle.transform.position.x > m_owner.transform.position.x);

                await UniTask.Yield(cancellationToken: token);
            }
        }

        // 창이 손에서 떨어질때까지 기다려야 해
        await UniTask.WaitForSeconds(0.166f, cancellationToken: token);

        // 하늘에서 창 떨어트리자
        var scale = m_element.animSkillJump.transform.localScale;
        scale.x = m_owner.move.isFlip ? -1 : 1;
        m_element.animSkillJump.transform.localScale = scale;

        m_element.warning_Circle.transform.SetParent(m_owner.transform.parent);
        m_element.animSkillJump.transform.SetParent(m_owner.transform.parent);
        m_element.animSkillJump.transform.position = m_element.warning_Circle.transform.position;
        m_element.animSkillJump.CrossFade("On", 0);

        var startJump = Time.time + m_waitJumpFrame / 60f;
        //데미지 줄거야
        {
            await UniTask.WaitForSeconds(5 / 60f);

            var damage = m_owner.stat.attackPower * 2;
            for (int i = 0; i < m_element.warning_Circle.target.Count; i++)
            {
                var target = m_element.warning_Circle.target[i];
                target.OnDamage(m_owner, damage);
            }
        }

        await UniTask.WaitUntil(() => Time.time >= startJump);
        //await UniTask.WaitUntil(() => m_element.animSkillJump.GetCurrentAnimatorStateInfo(0).IsName("Wait"), cancellationToken: token);

        // 점프
        {
            m_owner.anim.Play("Boss_Skill_Jin_Jump");

            var startTime = Time.time;
            //var sqrDistance = (m_owner.transform.position - m_element.animSkillJump.transform.position).sqrMagnitude;
            await DOTween.To(() => m_owner.transform.position, _pos => m_owner.rig.MovePosition(_pos), m_element.animSkillJump.transform.position, m_durationJump)
                 .SetEase(Ease.InQuint).OnUpdate(() =>
                 {
                     var progress = (Time.time - startTime) / m_durationJump;

                     var pos = m_owner.element.parts.localPosition;
                     pos.y = m_curveJump.Evaluate(progress) * 3f;
                     m_owner.element.parts.localPosition = pos;

                     //float percent = (m_owner.transform.position - m_element.animSkillJump.transform.position).sqrMagnitude / sqrDistance;
                 }).AsyncWaitForCompletion();

            m_owner.element.parts.localPosition = Vector3.zero;
        }

        //await UniTask.WaitUntil(() => Input.GetKey(KeyCode.P), cancellationToken: token);

        m_element.animSkillJump.CrossFade("Off", 0);
        m_owner.transform.position = m_element.animSkillJump.transform.position;
        m_owner.anim.Play("Boss_Skill_Jin_Jump_End");

        // 데미지
        {
            var damage = m_owner.stat.attackPower * 7;
            CameraManager.instance.Shake();
            Swing_KnockbackCharacter(4, damage);
        }

        await UniTask.WaitForSeconds(1f, cancellationToken: token);

        m_element.warning_Circle.SetDisable();

        m_element.warning_Circle.transform.SetParent(m_owner.transform);
        m_element.animSkillJump.transform.SetParent(m_owner.transform);

        m_owner.buff.Remove(hashDebuff);

        m_skillType = BossRaidSkillType_LuBu.NONE;
    }

    async UniTask SkillAsync_Swing()
    {
        var token = m_cts.Token;
        if (m_owner.target.target?.isLive == true)
        {
            m_skillType = BossRaidSkillType_LuBu.Swing;

            m_owner.move.MoveStop();
            var hashDebuff = m_owner.buff.Add(BuffType.DEBUFF_NO_MOVE);

            //기모으기는 1초
            m_element.warning_Circle.transform.localScale = new Vector3(3.571428f, 3.571428f, 1);
            m_element.warning_Circle.ShowAsync(1, token).Forget();
            m_owner.anim.Play(CharacterAnimType.Skill);

            await UniTask.NextFrame(cancellationToken: token);

            await UniTask.WaitForSeconds(m_owner.anim.GetStateInfo().length, cancellationToken: token);

            m_owner.buff.Remove(hashDebuff);
            m_skillType = BossRaidSkillType_LuBu.NONE;
        }
        else
            await UniTask.WaitUntil(() => m_owner.target.target?.isLive == true);
    }

    void Swing_KnockbackCharacter(float _distanceKnockback, float _damage)
    {
        float weidhtKnockback = .5f; // 가중치

        List<CharacterComponent> heroes = new();
        TeamManager.instance.GetHeroes(heroes, true);

        var posBoss = transform.position;

        CameraManager.instance.Shake();
        //var damage = m_owner.stat.attackPower * 5;

        for (int i = 0; i < heroes.Count; i++)
        {
            var target = heroes[i];

            var lookAt = target.transform.position - posBoss;

            if (lookAt == Vector3.zero)
                lookAt.x = m_owner.move.isFlip ? 1 : -1;

            var distance = lookAt.magnitude;

            if (m_element.warning_Circle.Contains(target))
            {
                target.OnDamage(m_owner, _damage, true);

                var bonusDistance = (_distanceKnockback - distance) * weidhtKnockback;
                Vector3 targetKnocback = posBoss + lookAt.normalized * (_distanceKnockback + bonusDistance);
                targetKnocback.z = target.transform.position.z;

                DOTween.To(() => target.transform.position, _pos => target.rig.MovePosition(_pos), targetKnocback, 0.2f);
            }
        }
    }

    public override void EventAttackHit(CharacterComponent _owner)
    {
        if (m_skillType == BossRaidSkillType_LuBu.NONE)
            base.EventAttackHit(_owner);
        else if (m_skillType == BossRaidSkillType_LuBu.Swing)
            Swing_KnockbackCharacter(7, m_owner.stat.attackPower * 5);
    }

    public override void OnManualValidate()
    {
        m_element.Initialize(transform);
        base.OnManualValidate();
    }

    [SerializeField, HideInInspector]
    ElementData_LuBuBossRaid m_element;

    [Serializable]
    struct ElementData_LuBuBossRaid
    {
        public WarningAreaComponent warning_Circle;
        public Animator animSkillJump;

        public void Initialize(Transform _transform)
        {
            warning_Circle = _transform.parent.GetComponent<WarningAreaComponent>("Fx_Warning_Circle");
            animSkillJump = _transform.parent.GetComponent<Animator>("Fx_Skill_Jump");
        }
    }
}
