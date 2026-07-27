using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Commander_ShiHuangdi_DailyDungeon : Weapon_Commander_ShiHuangdi
{
    int m_countMinion = 2;
    protected override void Start()
    {
        foreach (var m in m_element.minions)
            m.gameObject.SetActive(false);

        StartSkillAsync().Forget();
    }

    async UniTask StartSkillAsync()
    {
        await UniTask.WaitForSeconds(10f);
        SkillAsync_MinionRush().Forget();

        //await UniTask.WaitForSeconds(20f);
        //m_countMinion++;
        //SkillAsync_MinionRush().Forget();

        //await UniTask.WaitForSeconds(20f);
        //m_countMinion++;
        //SkillAsync_MinionRush().Forget();
    }

    async UniTask SkillAsync_MinionRush()
    {
        var mainHero = TeamManager.instance.mainHero;

        m_owner.move.MoveStop();
        var hashDebuff = m_owner.buff.Add(BuffType.DEBUFF_NO_MOVE);

        //m_owner.element.collider.enabled = false;

        m_owner.anim.Play("Skill_Cast");

        await UniTask.WaitForSeconds(40 / 60f);

        // 일어서라
        var randomIdx = RandomIndex();
        for (int i = 0; i < randomIdx.Length; i++)
        {
            int idx = randomIdx[i];
            var minion = m_element.minions[idx];
            minion.gameObject.SetActive(true);
            minion.transform.localPosition = m_element.orinLocalPosition[idx];

            await UniTask.WaitForSeconds(UnityEngine.Random.Range(0.1f, .3f));
        }

        randomIdx = RandomIndex();
        for (int i = 0; i < randomIdx.Length; i++)
        {
            await UniTask.WaitForSeconds(UnityEngine.Random.Range(1f, 2f));

            RushMinionAsync(m_element.minions[randomIdx[i]]).Forget();
        }

        await UniTask.WaitUntil(() => Input.GetKey(KeyCode.Escape));

        m_owner.buff.Remove(BuffType.DEBUFF_NO_MOVE, hashDebuff);
    }

    int[] RandomIndex()
    {
        int[] result = new int[m_countMinion];
        for (int i = 0; i < m_countMinion; i++)
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

    async UniTask RushMinionAsync(Animator _minion)
    {
        _minion.CrossFade("Attack_Cast", 0);

        var endTime = Time.time + 1;

        while (endTime > Time.time || Input.GetKey(KeyCode.P) == false)
        {
            var targetPos = TeamManager.instance.mainHero;

            var scale = _minion.transform.localScale;
         
            // left == 
            if (scale.x > 0 == targetPos.position.x > _minion.transform.position.x)
            {
                scale.x *= -1;
                _minion.transform.localScale = scale;
            }

            await UniTask.NextFrame(cancellationToken: destroyCancellationToken);
        }


        //m_owner.move.SetFlip(_targetPos.x > m_owner.position.x);

        //DateTime dt = DateTime.Now.AddSeconds(0.1f);
        //EffectWorker.instance.Dash(m_owner, m_owner.move.isFlip);

        //if (_isCameraShake)
        //    CameraManager.instance.Shake();

        //m_owner.anim.AttackMotionFirstFrame(CharacterAnimType.Attack_Move, 1);
        //await DOTween.To(() => m_owner.position, _pos => m_owner.rig.MovePosition(_pos), _targetPos, 0.2f).SetUpdate(UpdateType.Fixed)
        //    .OnUpdate(() =>
        //    {
        //        if (DateTime.Now > dt)
        //        {
        //            EffectWorker.instance.Dash(m_owner, m_owner.move.isFlip);
        //            dt = DateTime.Now.AddSeconds(10);

        //            m_owner.anim.animSpeed = 1f;
        //            m_owner.attack.ShowSlashEffect(true);
        //        }
        //    });
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
        public Animator[] minions;
        public List<Vector3> orinLocalPosition;
        public void Initialize(Transform _transform)
        {
            minions = _transform.Find("Minions").GetComponentsInChildren<Animator>();
            orinLocalPosition = new();
            for (int i = 0; i < minions.Length; i++)
                orinLocalPosition.Add(minions[i].transform.localPosition);
        }
    }
    #endregion VALIDATE

}
