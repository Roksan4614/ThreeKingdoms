using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;

public class Weapon_Champion_Guanyu : Weapon_Champion
{
    // 원에서부터 거리
    const float c_maxSqrMagnitudeRange = 14;

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

    CancellationTokenSource m_ctsMoveSkill;
    CancellationTokenSource m_ctsUseSkill;

    protected override void OnDestroy()
    {
        m_ctsMoveSkill = m_ctsMoveSkill.ReleaseCTS();
        m_ctsUseSkill = m_ctsUseSkill.ReleaseCTS();
        base.OnDestroy();
    }

    Color m_colorTargetting;
    public override bool IsValidUseSkill()
    {
        // 컨트롤 스킬로 up을 했다면, 적이 없어도 거기로 날라가자.
        if (m_isUseSkillControll == true)
            return true;

        // 그냥 사용하기를 눌렀다면, 사거리 안에 적이 있어야 사용하도록 하자.
        Vector3 ownerPos = m_owner.position;
        if (StageManager.instance.liveEnemyList
            .Where(x => (x.transform.position - ownerPos).sqrMagnitude < maxSqrMagnitue)
            .Count() > 0)
            return true;

        MoveAndUseSkill().Forget();

        return false;
    }

    public override void Die()
    {
        m_ctsMoveSkill = m_ctsMoveSkill.ReleaseCTS();
        m_ctsUseSkill = m_ctsUseSkill.ReleaseCTS();

        if (m_owner.isMain)
            ControllerManager.instance.SetSwitch(true);
        m_isUseSkillControll = false;
    }

    async UniTask MoveAndUseSkill()
    {
        m_ctsMoveSkill = m_ctsMoveSkill.ReleaseCTS(true);
        var token = m_ctsMoveSkill.Token;

        CharacterComponent target = null;
        while (ControllerManager.instance.isDoing == false)
        {
            var t = StageManager.instance.GetNearestEnemy(m_owner.position);
            if (t != target)
            {
                target = t;
                m_owner.move.MoveTarget(target, true);
            }

            if (target != null && (target.transform.position - m_owner.position).sqrMagnitude < maxSqrMagnitue)
            {
                //m_skillRange.position = target.transform.position;
                var index = TeamManager.instance.heroInfo.GetIndex(m_owner.info.key);
                TeamManager.instance.heroInfo.UseSkill(index);
                break;
            }

            await UniTask.NextFrame(cancellationToken: token);
        }
    }

    public override async UniTask UseSkillAsync()
    {
        m_ctsUseSkill = m_ctsUseSkill.ReleaseCTS(true);
        var token = m_ctsUseSkill.Token;

        // 그냥 스킬을 쓴거라면, 가장 가까운 적에게 날라가자.
        if (m_isUseSkillControll == false)
        {
            var enemy = StageManager.instance.GetNearestEnemy(m_owner.position);

            if (enemy == null)
                return;

            //var lookAt = m_owner.position - enemy.position;

            m_skillRange.position = enemy.transform.position;// + lookAt.normalized * 1.5f;
        }

        Vector3 targetPos = m_skillRange.position;

        if (m_owner.isMain)
        {
            ControllerManager.instance.SetPunchSkill();
            ControllerManager.instance.SetSwitch(false);
        }

        m_owner.move.MoveStop();
        m_owner.move.SetFlip(targetPos.x > m_owner.position.x);
        m_owner.anim.AttackMotionFirstFrame(CharacterAnimType.Attack_Move, 1);

        DateTime dt = DateTime.Now.AddSeconds(0.1f);
        EffectWorker.instance.Dash(m_owner, m_owner.move.isFlip);

        m_owner.element.collider.enabled = false;

        var m_tweenSkillMove = DOTween.To(() => m_owner.position, _pos => m_owner.rig.MovePosition(_pos), targetPos, 0.2f).SetUpdate(UpdateType.Fixed)
            .OnUpdate(() =>
            {
                UpdateEnemyStatus();

                if (DateTime.Now > dt)
                {
                    EffectWorker.instance.Dash(m_owner, m_owner.move.isFlip);
                    dt = DateTime.Now.AddSeconds(10);

                    m_owner.anim.AttackMotionEnd();
                    m_owner.attack.ShowSlashEffect(true);
                }
            });

        await m_tweenSkillMove.ToUniTask(TweenCancelBehaviour.Kill, token);

        m_owner.element.collider.enabled = true;

        bool isTargetting = false;
        var damage = m_owner.stat.attackPower * 2;
        var enemyList = StageManager.instance.enemyList;
        for (int i = 0; i < enemyList.Count; i++)
        {
            var target = enemyList[i];

            if (target.isLive == true && (target.transform.position - targetPos).sqrMagnitude < c_maxSqrMagnitudeRange)
            {
                isTargetting = true;
                target.OnDamage(m_owner, damage, true);

                target.buff.Add(BuffType.DEBUFF_NO_MOVE, _duration: 0.1f);

                //적들을 관우쪽으로 끌어당기기
                if (enemyList.Count > 1)
                    target.transform.DOMove(Vector3.Lerp(target.transform.position, m_owner.position, 0.5f), 0.1f).Forget();
            }

            target.SetColorParts(Color.white);
        }

        await UniTask.WaitUntil(
            () => m_owner.attack.isRunningAttack == false && m_owner.attack.isRunningSlash == false, cancellationToken: token);

        if (isTargetting == true)
            m_owner.move.MoveTarget(StageManager.instance.GetNearestEnemy(targetPos), true);

        m_isUseSkillControll = false;
        if (m_owner.isMain)
            ControllerManager.instance.SetSwitch(true);
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

    public override void OnDrag_ControllSkill(Vector3 _targetPos)
    {
        m_skillRange.gameObject.SetActive(true);

        var ownerPos = m_owner.position;
        var lookAt = Vector3.ClampMagnitude(_targetPos - ownerPos, m_maxMagnitude);

        m_skillRange.position = ownerPos + lookAt;

        UpdateEnemyStatus();
    }

    bool m_isUseSkillControll = false;
    public override void OnUp_ControllSkill()
    {
        if (m_skillRange.gameObject.activeSelf == true)
        {
            m_isUseSkillControll = true;
            m_skillRange.gameObject.SetActive(false);

            var index = TeamManager.instance.heroInfo.GetIndex(m_owner.info.key);
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
