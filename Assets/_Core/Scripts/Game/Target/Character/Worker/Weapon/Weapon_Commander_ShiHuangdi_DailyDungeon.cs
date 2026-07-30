using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Commander_ShiHuangdi_DailyDungeon : Weapon_Commander_ShiHuangdi
{
    protected override void Start()
    {
        foreach (var m in m_element.minion)
        {
            m.anim.gameObject.SetActive(false);
            m.warning.SetParent(MapManager.instance.transform);
        }

        StartSkillAsync().Forget();

        Signal.instance.DailyDungeonStatus.connectLambda = new(this, _status =>
        {
            if (_status == Data_DailyDungeon.DailyDungeonStatusType.Timeout)
                m_cts = m_cts.ReleaseCTS();
        });
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var m in m_element.minion)
            Destroy(m.warning.gameObject);

        m_cts = m_cts.ReleaseCTS();
    }

    async UniTask StartSkillAsync()
    {
        m_cts = m_cts.ReleaseCTS(true);

        await UniTask.WaitForSeconds(3f, cancellationToken: m_cts.Token);
        SkillAsync_MinionRush(2).Forget();

        while (DataManager.dailyDungeon.isRunning == true)
        {
            await UniTask.WaitForSeconds(10f, cancellationToken: m_cts.Token);
            SkillAsync_MinionRush(4).Forget();
        }
    }

    async UniTask SkillAsync_MinionRush(int _countMaxMinion)
    {
        var target = TeamManager.instance.mainHero;
        if (target.isLive == false)
            target = TeamManager.instance.GetFarthestHero(m_owner.position);

        if (target == null)
        {
            await UniTask.NextFrame(m_cts.Token);
            SkillAsync_MinionRush(_countMaxMinion).Forget();
            return;
        }

        m_owner.move.MoveStop();
        var hashDebuff = m_owner.buff.Add(BuffType.DEBUFF_NO_MOVE);

        m_owner.anim.Play("Skill_Cast");

        await UniTask.WaitForSeconds(40 / 60f, cancellationToken: m_cts.Token);

        // 일어서라
        var boss = StageManager.instance.boss_dailyDungeon;
        var randomIdx = RandomIndex(_countMaxMinion);
        m_element.parentMinion.position = boss.position;
        for (int i = 0; i < randomIdx.Length; i++)
        {
            int idx = randomIdx[i];
            var minion = m_element.minion[idx];
            minion.gameObject.SetActive(true);
            minion.anim.CrossFade("Raise", 0);

            var pos = minion.orinLocalPosition;

            var scale = minion.transform.localScale;

            // left == 
            if (scale.x > 0 == boss.move.isFlip)
            {
                scale.x *= -1;
                minion.transform.localScale = scale;
            }

            pos.x *= scale.x;
            minion.transform.localPosition = pos;

            await UniTask.WaitForSeconds(UnityEngine.Random.Range(0.1f, .3f), cancellationToken: m_cts.Token);
        }

        await UniTask.WaitForSeconds(.5f, cancellationToken: m_cts.Token);

        m_owner.anim.Play("Skill_Cast02");

        // 미니언 돌격 시작
        randomIdx = RandomIndex(_countMaxMinion);
        List<UniTask> tasksRush = new();
        for (int i = 0; i < randomIdx.Length; i++)
        {
            tasksRush.Add(RushMinionAsync(target, m_element.minion[randomIdx[i]]));
            await UniTask.WaitForSeconds(.5f);
        }

        await UniTask.WhenAll(tasksRush);

        // 돌격 끝

        m_owner.anim.Play("Skill_End");
        await UniTask.WaitForSeconds(1f);

        m_owner.buff.Remove(BuffType.DEBUFF_NO_MOVE, hashDebuff);
    }

    int[] RandomIndex(int _countMaxMinion)
    {
        int[] result = new int[_countMaxMinion];
        for (int i = 0; i < _countMaxMinion; i++)
            result[i] = i;

        for (int i = result.Length - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            int temp = result[i];
            result[i] = result[randomIndex];
            result[randomIndex] = temp;
        }
        return result;
    }

    async UniTask RushMinionAsync(CharacterComponent _target, MinionData _minion)
    {
        _minion.anim.CrossFade("Attack_Cast", 0);

        float duration = 1;
        var endTime = Time.time + duration * .9f;

        _minion.warning.transform.position = _minion.transform.position + Vector3.up * .2f;

        var scale = _minion.transform.localScale;

        _minion.warning.Show(duration, m_cts.Token, false);

        var targetPos = _target.position;
        while (endTime > Time.time)
        {
            if (_target.isLive == false)
                _target = TeamManager.instance.GetFarthestHero(_minion.transform.position);

            if (_target == null)
            {
                _minion.warning.SetActive(false);
                await UniTask.WaitForSeconds(.5f, cancellationToken: m_cts.Token);
                _minion.anim.CrossFade("Die", 0);
                return;
            }

            targetPos = _target.position;
            targetPos += (targetPos - _minion.transform.position).normalized * 3f;

            _minion.warning.SetLookTarget_Box(targetPos);

            // left == 
            if (scale.x > 0 == targetPos.x > _minion.transform.position.x)
            {
                scale.x *= -1;
                _minion.transform.localScale = scale;
            }

            await UniTask.NextFrame(m_cts.Token);
        }

        await UniTask.WaitForSeconds(.5f, cancellationToken: m_cts.Token);

        DateTime dt = DateTime.Now.AddSeconds(0.1f);
        EffectWorker.instance.Dash(m_owner, m_owner.move.isFlip);

        _minion.anim.CrossFade("Attack_Run", 0);

        var damage = StageManager.instance.boss_dailyDungeon.stat.attackPower * 5;
        for (int i = 0; i < _minion.warning.target.Count; i++)
        {
            var target = _minion.warning.target[i];

            if (target is Character_Enemy || target.isLive == false)
                continue;

            target.OnDamage(null, damage);

            EffectWorker.instance.SlotDamageTakenEffect(new()
            {
                attacker = _minion.transform,
                target = target,
                value = -damage,
                isCritical = false,
                isAlliance = true
            });

            // 뒤로 살짝 밀치자
            var lookAt = target.position - _minion.transform.position;
            target.rig.MovePosition(target.position + lookAt.normalized * 2);
        }

        _minion.warning.SetActive(false);

        DOTween.To(() => _minion.transform.position, _pos => _minion.transform.position = _pos, targetPos, 0.2f)
            .SetUpdate(UpdateType.Fixed)
            .OnUpdate(() =>
            {
                if (DateTime.Now > dt)
                {
                    EffectWorker.instance.Dash(m_owner, m_owner.move.isFlip);
                    dt = DateTime.Now.AddSeconds(10);
                }
            }).OnComplete(() =>
            {
                _minion.anim.CrossFade("Attack_End", 0);
                Utils.AfterSecond(() => _minion.anim.CrossFade("Die", 0), 1f);
            }).ToUniTask(cancellationToken: m_cts.Token).Forget();
    }

    #region VALIDATE
    public override void OnManualValidate()
    {
        m_element.Initialize(transform);
        base.OnManualValidate();
    }

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public List<MinionData> minion;
        public void Initialize(Transform _transform)
        {
            var parent = _transform.parent.parent.Find("Minions");

            minion = new();
            for (int i = 0; i < parent.childCount; i++)
            {
                var slot = parent.GetChild(i);
                minion.Add(new()
                {
                    anim = slot.GetComponent<Animator>(),
                    orinLocalPosition = slot.localPosition,
                    warning = slot.GetComponent<WarningAreaComponent>("Warning_Box")
                });
            }
        }
        public Transform parentMinion => minion[0].transform.parent;
    }

    [Serializable]
    struct MinionData
    {
        public Animator anim;
        public Vector3 orinLocalPosition;
        public WarningAreaComponent warning;

        public Transform transform => anim.transform;
        public GameObject gameObject => anim.gameObject;
    }

    #endregion VALIDATE

}
