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

    [SerializeField] bool m_isJIN;
    [SerializeField] AnimationCurve m_curveJump;
    int m_waitJumpFrame = 30;
    float m_durationJump = .5f;

    List<Skilldata> m_dbSkills = new();

    protected override void Start()
    {
        if (m_element.animSkillJump)
            m_element.animSkillJump.gameObject.SetActive(true);

        if (BossRaidWorker.instance.isRunning == false || StageManager.instance == null)
            return;

        m_element.warning_Circle.transform.SetParent(m_owner.transform.parent);
        m_element.warning_RedHare.transform.parent.SetParent(m_owner.transform.parent);
        m_element.animSkillJump.transform.SetParent(m_owner.transform.parent);

        m_dbSkills = new()
        {
            new Skilldata()
            {
                duration = 5,
                async = SkillAsync_Swing
                //async = SkillAsync_RedHare
            },

            new Skilldata()
            {
                duration = 10,
                async = SkillAsync_RedHare
            },
        };

        if (m_isJIN)
            m_dbSkills.Add(new()
            {
                duration = 20,
                async = SkillAsync_Jump
            });

        Signal.instance.BossRaidStatus.connect = SlotBossRaidStatus;
        SkillAsync().Forget();
    }

    void SlotBossRaidStatus(BossRaidStatusType _statusType)
    {
        switch (_statusType)
        {
            case BossRaidStatusType.Finish_FirstPhase:
            case BossRaidStatusType.Finished:
                m_cts = m_cts.ReleaseCTS();
                m_element.warning_Circle.transform.SetParent(m_owner.transform.parent);
                m_element.warning_Circle.SetActive(false);
                m_element.warning_RedHare.transform.parent.SetParent(m_owner.transform.parent);
                m_element.warning_RedHare.SetActive(false);
                break;
        }
    }

    struct Skilldata
    {
        public float timeAction;
        public float duration;
        public Func<UniTask> async;
    }

    async UniTask SkillAsync()
    {
        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;

        await UniTask.NextFrame(cancellationToken: token);

        CameraManager.instance.SetCameraPosTarget(m_owner.cameraPos);

        await UniTask.WaitUntil(() => DataManager.bossRaid.raidStatus == BossRaidStatusType.FirstPhase || DataManager.bossRaid.raidStatus == BossRaidStatusType.SecondPhase,
            cancellationToken: token);

        var dbSkill = new List<Skilldata>(m_dbSkills);

        var timeStart = Time.time + UnityEngine.Random.Range(3f, 6f);
        for (int i = 0; i < dbSkill.Count; i++)
        {
            var skill = dbSkill[i];
            skill.timeAction = timeStart + i;
            dbSkill[i] = skill;
        }

        while (true)
        {
            m_skillType = BossRaidSkillType_LuBu.NONE;

            var skill = dbSkill[0];

            while (skill.timeAction > Time.time)
                await UniTask.NextFrame(cancellationToken: token);

            while (TeamManager.instance.GetRandomHero(true) == null)
                await UniTask.NextFrame(cancellationToken: token);

            // 모두 죽어있으면 기다리자
            if (TeamManager.instance.GetRandomHero(true) == null)
            {
                await UniTask.WaitUntil(() => TeamManager.instance.GetRandomHero(true) != null, cancellationToken: token);
                await UniTask.WaitForSeconds(.5f);
            }

            await skill.async();

            //스킬을 쓰면 일단 조금은 기다리자.
            skill.timeAction = Time.time + skill.duration;
            await UniTask.WaitForSeconds(UnityEngine.Random.Range(3f, 6f), cancellationToken: token);

            dbSkill[0] = skill;
            dbSkill = dbSkill.SortBy(x => x.timeAction);
        }
    }

    float m_speedCircleMove = 20;
    async UniTask SkillAsync_Jump()
    {
        var token = m_cts.Token;

        m_skillType = BossRaidSkillType_LuBu.Jump;

        m_owner.move.MoveStop();
        var hashDebuff = m_owner.buff.Add(BuffType.DEBUFF_NO_MOVE);

        // 가운데로 대쉬함 하자
        {
            Vector3 targetPos = m_owner.position;
            targetPos.x += m_owner.move.isFlip ? -1 : 1;

            await m_owner.move.DashAsync(targetPos);
        }

        // 차징시작
        m_owner.anim.Play("Boss_Skill_Jin");

        // 루프중이야?
        // 랜덤 타겟
        //var targetJump = TeamManager.instance.GetRandomHero(true);
        // 주장이 타겟
        var targetJump = TeamManager.instance.mainHero;

        if (targetJump.isLive == false)
            targetJump = TeamManager.instance.GetFarthestHero(m_owner.position);

        await UniTask.WaitUntil(() =>
        {
            m_owner.move.SetFlip(targetJump.transform.position.x > m_owner.position.x);
            return m_owner.anim.IsType("Boss_Skill_Jin_Charge");
        }, cancellationToken: token);

        CameraManager.instance.SetCameraPosTarget(targetJump.cameraPos, false);
        // 그럼 영역생성
        m_element.warning_Circle.transform.localScale = new Vector3(2.68f, 2.68f, 1);
        m_element.warning_Circle.ShowAsync(1, token, false).Forget();

        // 영역이 영웅을 따라다녀야 해
        {
            var endTime = Time.time + 1;

            // 뒤에 장수가 있어?
            m_element.warning_Circle.transform.position = m_owner.position;

            while (Time.time < endTime)
            {
                Vector3 currentPos = m_element.warning_Circle.transform.position;
                Vector3 targetPos = targetJump.transform.position;

                m_element.warning_Circle.transform.position = Vector3.MoveTowards(currentPos, targetPos, m_speedCircleMove * Time.deltaTime);

                m_owner.move.SetFlip(m_element.warning_Circle.transform.position.x > m_owner.position.x);

                await UniTask.NextFrame(cancellationToken: token);
            }
        }

        // 창이 손에서 떨어질때까지 기다려야 해
        await UniTask.WaitForSeconds(0.166f, cancellationToken: token);

        // 하늘에서 창 떨어트리자
        var scale = m_element.animSkillJump.transform.localScale;
        scale.x = m_owner.move.isFlip ? -1 : 1;
        m_element.animSkillJump.transform.localScale = scale;

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

        // 점프
        {
            m_owner.anim.Play("Boss_Skill_Jin_Jump");

            await UniTask.WaitForSeconds(10 / 60f);

            var startTime = Time.time;
            await DOTween.To(() => m_owner.position, _pos => m_owner.rig.MovePosition(_pos), m_element.animSkillJump.transform.position, m_durationJump)
                .SetUpdate(UpdateType.Fixed)
                .SetEase(Ease.InQuint).OnUpdate(() =>
                {
                    var progress = (Time.time - startTime) / m_durationJump;

                    var pos = m_owner.element.parts.localPosition;
                    pos.y = m_curveJump.Evaluate(progress) * 3f;
                    m_owner.element.parts.localPosition = pos;

                }).AsyncWaitForCompletion();

            m_owner.element.parts.localPosition = Vector3.zero;
        }

        CameraManager.instance.SetCameraPosTarget(m_owner.cameraPos, false);

        m_element.animSkillJump.CrossFade("Off", 0);
        m_owner.position = m_element.animSkillJump.transform.position;
        m_owner.anim.Play("Boss_Skill_Jin_Jump_End");

        // 데미지
        {
            var damage = m_owner.stat.attackPower * 7;
            CameraManager.instance.Shake();
            KnockbackCharacter(4, damage);
        }

        await UniTask.WaitForSeconds(1f, cancellationToken: token);

        m_element.warning_Circle.SetActive(false);

        m_owner.buff.Remove(hashDebuff);

        m_skillType = BossRaidSkillType_LuBu.NONE;
    }

    async UniTask SkillAsync_RedHare()
    {
        var hashNoMove = m_owner.buff.Add(BuffType.DEBUFF_NO_MOVE);
        var hashNoDie = m_owner.buff.Add(BuffType.BUFF_NO_DIE);

        var token = m_cts.Token;
        await UniTask.NextFrame(cancellationToken: token);

        m_owner.move.MoveStop();
        {
            Vector3 targetPos = m_owner.position;
            targetPos.x += m_owner.move.isFlip ? -1 : 1;

            await m_owner.move.DashAsync(targetPos);
        }

        // 말 태우자
        m_element.mount.SetMount(m_owner, true);
        await UniTask.WaitForSeconds(.5f, cancellationToken: token);

        var waring = m_element.warning_RedHare.transform.parent;
        waring.gameObject.SetActive(true);

        int countMax = DataManager.bossRaid.data.tickSecondPhase > 0 ? 3 : 1;

        for (int i = 0; i < countMax; i++)
        {
            var target = TeamManager.instance.mainHero;
            if (target.isLive == false)
                target = TeamManager.instance.GetFarthestHero(m_owner.position);

            if (target == null)
                break;

            //준비
            m_owner.anim.Play("Mount_Skill");
            m_element.mount.Play("Skill");

            // 영역 표시
            m_element.warning_RedHare.transform.parent.position = m_owner.position;
            m_element.warning_RedHare.ShowAsync(1, token, false).Forget();

            var endTime = Time.time + .8f;
            while (endTime >= Time.time)
            {
                m_owner.move.SetFlip(m_owner.position.x < target.position.x);

                var lookAt = target.position - transform.position;
                float angle = Mathf.Atan2(lookAt.y, lookAt.x) * Mathf.Rad2Deg;
                waring.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                await UniTask.NextFrame(cancellationToken: token);
            }

            var distance = (m_owner.position - m_element.targetRedHare.position).sqrMagnitude;

            for (int idxTarget = 0; idxTarget < m_element.warning_RedHare.target.Count; idxTarget++)
            {
                var t = m_element.warning_RedHare.target[idxTarget];
                var p = (t.position - m_owner.position).sqrMagnitude / distance;

                Utils.AfterSecond(() =>
                {
                    t.OnDamage(m_owner, m_owner.stat.attackPower * 6, true);
                }, m_rushDuration * p);
            }

            await m_owner.transform.DOMove(m_element.targetRedHare.position, m_rushDuration)
                .SetEase(Ease.OutQuad)
                .ToUniTask(TweenCancelBehaviour.Kill, token);

            m_element.mount.Play("Skill_End");
            m_owner.anim.Play("Mount_Skill_End");

            m_element.warning_RedHare.SetActive(false);
            m_owner.move.SetFlip(m_owner.position.x < target.position.x);

            await UniTask.WaitForSeconds(.5f, cancellationToken: token);
        }

        await UniTask.WaitForSeconds(.5f, cancellationToken: token);

        // 말 내리기
        m_element.mount.SetMount(m_owner, false);

        m_owner.buff.Remove(BuffType.DEBUFF_NO_MOVE, hashNoMove);
        m_owner.buff.Remove(BuffType.BUFF_NO_DIE, hashNoDie);

        waring.gameObject.SetActive(false);
    }

    [SerializeField] float m_rushDuration = 1f;

    async UniTask SkillAsync_Swing()
    {
        if (m_cts == null)
            return;

        var token = m_cts.Token;
        while (m_owner.target.nearestEnemy == null)
        {
            var target = TeamManager.instance.GetNearestHero(m_owner.position);
            Vector3 targetPos = target.position;
            await m_owner.move.DashAsync(targetPos);
        }

        await UniTask.NextFrame(cancellationToken: token);

        m_skillType = BossRaidSkillType_LuBu.Swing;

        m_owner.move.MoveStop();
        var hashNoDie = m_owner.buff.Add(BuffType.BUFF_NO_DIE);
        var hashDebuff = m_owner.buff.Add(BuffType.DEBUFF_NO_MOVE);

        //기모으기는 1초
        m_element.warning_Circle.transform.position = m_owner.position;
        m_element.warning_Circle.transform.localScale = new Vector3(3.571428f, 3.571428f, 1);
        m_element.warning_Circle.ShowAsync(1, token).Forget();
        m_owner.anim.Play(CharacterAnimType.Skill);

        await UniTask.NextFrame(cancellationToken: token);

        await UniTask.WaitForSeconds(m_owner.anim.GetStateInfo().length, cancellationToken: token);

        m_owner.buff.Remove(BuffType.DEBUFF_NO_MOVE, hashDebuff);
        m_owner.buff.Remove(BuffType.BUFF_NO_DIE, hashNoDie);
        m_skillType = BossRaidSkillType_LuBu.NONE;
    }

    void KnockbackCharacter(float _distanceKnockback, float _damage)
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
                // 죽었으면 뒤로 밀치지 말자
                if (target.OnDamage(m_owner, _damage, true) == true)
                    continue;

                var bonusDistance = (_distanceKnockback - distance) * weidhtKnockback;
                Vector3 targetKnocback = posBoss + lookAt.normalized * (_distanceKnockback + bonusDistance);
                targetKnocback.z = target.transform.position.z;

                DOTween.To(() => target.transform.position, _pos =>
                {
                    target.rig.MovePosition(_pos);

                }, targetKnocback, 0.2f).SetUpdate(UpdateType.Fixed);
            }
        }
    }

    public override void EventAttackHit(CharacterComponent _owner)
    {
        if (m_skillType == BossRaidSkillType_LuBu.NONE)
            base.EventAttackHit(_owner);
        else if (m_skillType == BossRaidSkillType_LuBu.Swing)
            KnockbackCharacter(7, m_owner.stat.attackPower * 5);
    }
    public override void Die()
    {
        m_element.warning_Circle.SetActive(false);
        m_element.warning_RedHare.SetActive(false);

        ReleaseCTS();
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
        public WarningAreaComponent warning_RedHare;
        public Transform targetRedHare;

        public Animator animSkillJump;

        public MountComponent mount;

        public void Initialize(Transform _transform)
        {
            warning_Circle = _transform.parent.GetComponent<WarningAreaComponent>("Fx_Warning_Circle");

            warning_RedHare = _transform.parent.GetComponent<WarningAreaComponent>("Fx_Warning_RedHare/Fx_Warning");
            targetRedHare = warning_RedHare.transform.parent.Find("Target");

            animSkillJump = _transform.parent.GetComponent<Animator>("Fx_Skill_Jump");

            mount = _transform.GetComponent<MountComponent>("Mount/RedHare");
        }
    }
}
