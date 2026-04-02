using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class Weapon_Champion_Guanyu : Weapon_Champion
{
    // 원에서부터 거리
    const float c_maxSqrMagnitudeRange = 10;

    float m_maxMagnitude = 8;
    
    float m_maxSqrMagnitue = -1;
    float maxSqrMagnitue
    {
        get
        {
            if (m_maxSqrMagnitue == -1)
                m_maxSqrMagnitue = Mathf.Pow(m_maxMagnitude, 2);
            return m_maxSqrMagnitue;
        }
    }

    Color m_colorTargetting;
    public override bool IsValidUseSkill()
    {
        // 컨트롤 스킬로 up을 했다면, 적이 없어도 거기로 날라가자.
        if (m_isUseSkillControll == true)
            return true;

        // 그냥 사용하기를 눌렀다면, 사거리 안에 적이 있어야 사용하도록 하자.
        Vector3 ownerPos = m_owner.transform.position;
        if (StageManager.instance.liveEnemyList
            .Where(x => (x.transform.position - ownerPos).sqrMagnitude < maxSqrMagnitue)
            .Count() > 0)
            return true;

        MoveAndUseSkill().Forget();

        return false;
    }

    async UniTask MoveAndUseSkill()
    {
        CharacterComponent target = null;
        while (ControllerManager.instance.isDoing == false)
        {
            var t = StageManager.instance.GetNearestEnemy(m_owner.transform.position);
            if (t != target)
            {
                target = t;
                m_owner.move.MoveTarget(target, true);
            }

            if ((target.transform.position - m_owner.transform.position).sqrMagnitude < maxSqrMagnitue)
            {
                var index = TeamManager.instance.heroInfo.GetIndex(m_owner.data.key);
                TeamManager.instance.heroInfo.UseSkill(index);
                break;
            }

            await UniTask.Yield();
        }
    }

    public override async UniTask UseSkillAsync()
    {
        ControllerManager.instance.SetPunchSkill();

        Vector3 targetPos = m_skillRange.position;

        // 그냥 스킬을 쓴거라면, 가장 가까운 적에게 날라가자.
        if (m_isUseSkillControll == false)
            targetPos = StageManager.instance.GetNearestEnemy(m_owner.transform.position).transform.position;

        ControllerManager.instance.isSwitch = false;

        m_owner.move.MoveStop();
        m_owner.move.SetFlip(targetPos.x > m_owner.transform.position.x);
        m_owner.anim.AttackMotionFirstFrame(_layerIndex: 1);

        DateTime dt = DateTime.Now.AddSeconds(0.1f);
        EffectWorker.instance.Dash(m_owner, m_owner.move.isFlip);

        await DOTween.To(() => m_owner.transform.position, _pos => m_owner.rig.MovePosition(_pos), targetPos, 0.2f).OnUpdate(() =>
        {
            UpdateEnemyStatus();

            if (DateTime.Now > dt)
            {
                EffectWorker.instance.Dash(m_owner, m_owner.move.isFlip);
                dt = DateTime.Now.AddSeconds(10);

                m_owner.anim.AttackMotionEnd();
                m_owner.attack.ShowSlashEffect(true);
            }
        }).AsyncWaitForCompletion();

        bool isTargetting = false;
        var damage = m_owner.data.attackPower * 2;
        var enemyList = StageManager.instance.enemyList;
        for (int i = 0; i < enemyList.Count; i++)
        {
            var target = enemyList[i];

            if (target.isLive == true && (target.transform.position - targetPos).sqrMagnitude < c_maxSqrMagnitudeRange)
            {
                isTargetting = true;
                EffectWorker.instance.SlotDamageTakenEffect(new()
                {
                    attacker = m_owner.transform,
                    target = target,
                    value = -damage,
                    isCritical = true,
                    isAlliance = target.factionType == FactionType.Alliance
                });
                target.OnDamage(m_owner, damage);
            }

            target.SetColorParts(Color.white);
        }

        await UniTask.WaitUntil(
            () => m_owner.attack.isRunningAttack == false && m_owner.attack.isRunningSlash == false);

        if (isTargetting == true)
            m_owner.move.MoveTarget(StageManager.instance.GetNearestEnemy(targetPos), true);

        m_isUseSkillControll = false;
        ControllerManager.instance.isSwitch = true;
    }

    public override void OnDrag_ControllSkill(Vector3 _targetPos)
    {
        m_skillRange.gameObject.SetActive(true);

        var ownerPos = m_owner.transform.position;
        var lookAt = Vector3.ClampMagnitude(_targetPos - ownerPos, m_maxMagnitude);

        m_skillRange.position = ownerPos + lookAt;
        UpdateEnemyStatus();
    }

    void UpdateEnemyStatus()
    {
        if (m_colorTargetting == default)
            ColorUtility.TryParseHtmlString("#C3C3C3", out m_colorTargetting);

        var enemies = StageManager.instance.enemyList;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            bool isTargetting =
                (e.transform.position - m_skillRange.position).sqrMagnitude < c_maxSqrMagnitudeRange;

            e.SetColorParts(isTargetting == true ? m_colorTargetting : Color.white);
        }
    }

    bool m_isDrag = false;
    bool m_isUseSkillControll = false;
    public override void OnUp_ControllSkill()
    {
        if (m_skillRange.gameObject.activeSelf == true)
        {
            m_isUseSkillControll = true;
            m_skillRange.gameObject.SetActive(false);

            var index = TeamManager.instance.heroInfo.GetIndex(m_owner.data.key);
            TeamManager.instance.heroInfo.UseSkill(index);
        }
    }

    public override void OnCancel_ControllSkill()
    {
        m_isUseSkillControll = false;
        m_skillRange.gameObject.SetActive(false);

        var enemyList = StageManager.instance.enemyList;
        for (int i = 0; i < enemyList.Count; i++)
            enemyList[i].SetColorParts(Color.white);
    }
}
