using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using static Data_BossRaid;

public class Weapon_Vanguard_Lubu_BossRaid : Weapon_Vanguard_Lubu
{
    enum BossRaidSkillType_LuBu
    {
        NONE,
        Swing
    }

    BossRaidSkillType_LuBu m_skillType;

    protected override void Awake()
    {
        base.Awake();

        Signal.instance.BossRaidStatus.connect = SlotBossRaidStatus;
    }

    void SlotBossRaidStatus(BossRaidStatusType _statusType)
    {
        switch (_statusType)
        {
            case BossRaidStatusType.FirstPhase:
            case BossRaidStatusType.SecondPhase:
                SkillAsync().Forget();
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

        while (true)
        {
            m_skillType = BossRaidSkillType_LuBu.NONE;
            await UniTask.WaitForSeconds(3f, cancellationToken: token);

            await SkillAsync_Swing();
        }
    }

    async UniTask SkillAsync_Swing()
    {
        var token = m_cts.Token;
        if (m_owner.target.target?.isLive == true)
        {
            m_skillType = BossRaidSkillType_LuBu.Swing;

            m_owner.move.MoveStop();
            var hashDebuff = m_owner.buff.Add(BuffType.DEBUFF_NO_MOVE);

            m_owner.anim.Play(CharacterAnimType.Skill);

            await UniTask.NextFrame(cancellationToken: token);

            await UniTask.WaitForSeconds(m_owner.anim.GetStateInfo().length, cancellationToken: token);

            m_owner.buff.Remove(hashDebuff);
        }
    }

    public override void EventAttackHit(CharacterComponent _owner)
    {
        if (m_skillType == BossRaidSkillType_LuBu.Swing)
            Swing_KnockbackCharacter();
        else
            base.EventAttackHit(_owner);
    }

    void Swing_KnockbackCharacter()
    {
        var distanceKnockback = 7; //뒤로 밀려나는 거리
        float weidhtKnockback = .5f; // 가중치

        List<CharacterComponent> heroes = new();
        TeamManager.instance.GetHeroes(heroes, true);

        var posBoss = transform.position;

        CameraManager.instance.Shake();
        var damage = m_owner.stat.attackPower * 5;

        for (int i = 0; i < heroes.Count; i++)
        {
            var target = heroes[i];

            var lookAt = target.transform.position - posBoss;
            var distance = lookAt.magnitude;

            if (distance < distanceKnockback)
            {
                EffectWorker.instance.SlotDamageTakenEffect(new()
                {
                    attacker = m_owner.transform,
                    target = target,
                    value = -damage,
                    isCritical = true,
                    isAlliance = target.factionType == FactionType.Alliance
                });
                target.OnDamage(m_owner, damage);
                var bonusDistance = (distanceKnockback - distance) * weidhtKnockback;
                Vector3 targetKnocback = posBoss + lookAt.normalized * (distanceKnockback + bonusDistance);
                targetKnocback.z = target.transform.position.z;

                DOTween.To(() => target.transform.position, _pos => target.rig.MovePosition(_pos), targetKnocback, 0.2f);
            }
        }
    }

    public override void OnManualValidate()
    {
        base.OnManualValidate();
    }

    struct ElementData_LuBuBossRaid
    {

    }
}
