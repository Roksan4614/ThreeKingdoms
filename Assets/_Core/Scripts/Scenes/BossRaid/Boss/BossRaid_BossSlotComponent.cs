using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using static Data_BossRaid;

public class BossRaid_BossSlotComponent : MonoBehaviour, IValidatable
{
    float m_durationChange = 1.5f;
    float m_distanceKnockback = 7f;
    float m_weidhtKnockback = .5f;

    CancellationTokenSource m_cts;

    public Character_Enemy_RaidBoss boss =>
        DataManager.bossRaid.data.tickSecondPhase > 0 ? m_element.bossJIN : m_element.boss;

    protected virtual void Awake()
    {
#if UNITY_EDITOR
        if (Configure.instance.isBooted == false || StageManager.instance == null)
            return;
#endif

        m_element.boss.gameObject.SetActive(DataManager.bossRaid.raidStatus < BossRaidStatusType.Wait_SecondPhase);
        m_element.bossJIN.gameObject.SetActive(DataManager.bossRaid.raidStatus >= BossRaidStatusType.Wait_SecondPhase);

        boss.SetBossData(DataManager.bossRaid.data.keyBoss);

        Signal.instance.BossRaidStatus.connect = SlotBossRaidStatus;
    }

    private void Start()
        => ArrowNaviComponent.instance.SetParent(boss.element.parentCanvas);

    private void OnDestroy()
        => m_cts = m_cts.Release();

    void SlotBossRaidStatus(BossRaidStatusType _status)
    {
        m_cts = m_cts.Release(true);
        var token = m_cts.Token;

        switch (_status)
        {
            case BossRaidStatusType.FirstPhase:
                {
                    StageManager.instance.ClearEnemyList();
                    StageManager.instance.AddEnemyList(boss);
                }
                break;
            case BossRaidStatusType.Finish_FirstPhase:
            case BossRaidStatusType.Finished:
                {
                    ArrowNaviComponent.instance.SetTarget(null);
                    ArrowNaviComponent.instance.SetParent(MapManager.instance.transform);
                    boss.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);
                    ActionAsync_FinishPhase(_status).Forget();
                }
                break;
            case BossRaidStatusType.SecondPhase:
                {
                    ArrowNaviComponent.instance.SetTarget(TeamManager.instance.mainHero.transform);
                    ArrowNaviComponent.instance.SetParent(boss.element.parentCanvas);

                    TeamManager.instance.mainHero.buff.Remove(BuffType.BUFF_NO_TAKEN_DAMAGE);
                    boss.buff.Remove(BuffType.BUFF_NO_TAKEN_DAMAGE);

                    StageManager.instance.ClearEnemyList();
                    StageManager.instance.AddEnemyList(boss);

                    boss.SetBossData(DataManager.bossRaid.data.keyBoss);

                    TeamManager.instance.SetState(CharacterStateType.SearchEnemy);
                    StageManager.instance.SetState(CharacterStateType.Battle);
                }
                break;
        }
    }

    CancellationTokenSource m_ctsAction;
    async UniTask ActionAsync_FinishPhase(BossRaidStatusType _status)
    {
        m_ctsAction = m_ctsAction.Release(true);
        var token = m_ctsAction.Token;

        //StageManager.instance.SetState(CharacterStateType.None);
        //TeamManager.instance.SetState(CharacterStateType.None);

        InfoStageComponent.instance.SetActive(false, true);

        await UniTask.WaitForSeconds(1f, cancellationToken: token);

        (Scene_BossRaid.instance as Scene_BossRaid).SetActiveResult(true, true);

        await UniTask.WaitForSeconds(.5f, cancellationToken: token);

        if (_status == BossRaidStatusType.Finish_FirstPhase)
        {
            m_element.fxChangeBoss.transform.position = m_element.bossJIN.transform.position = m_element.boss.transform.position;
            m_element.fxChangeBoss.SetActive(true);
            if (boss.move.isFlip)
            {
                var scale = m_element.fxChangeBoss.transform.localScale;
                scale.x = -1;
                m_element.fxChangeBoss.transform.localScale = scale;
            }

            await UniTask.WaitForSeconds(m_durationChange, cancellationToken: token);

            m_element.bossJIN.gameObject.SetActive(true);
            m_element.bossJIN.position = m_element.boss.position;
            m_element.bossJIN.move.SetFlip(m_element.boss.move.isFlip);
            Destroy(m_element.boss.gameObject);

            //CameraManager.instance.SetCameraPosTarget(m_element.bossJIN.element.cameraPos);

            m_element.bossJIN.anim.Play("Boss_Die_End");

            (Scene_BossRaid.instance as Scene_BossRaid).SetActiveResult(false, true);

            await UniTask.WaitForSeconds(0.07f, cancellationToken: token);

            KnockbackCharacter();

            await UniTask.WaitForSeconds(1f, cancellationToken: token);

            BossRaidWorker.instance.Wait_SecondPhase();
        }
        else
        {
            PopupManager.instance.OpenPopup(PopupType.BossRaidResult);
        }
    }

    void KnockbackCharacter()
    {
        List<CharacterComponent> heroes = new();
        TeamManager.instance.GetHeroes(heroes, true);

        var posBoss = m_element.bossJIN.transform.position;

        CameraManager.instance.Shake();

        for (int i = 0; i < heroes.Count; i++)
        {
            var hero = heroes[i];

            var lookAt = hero.transform.position - posBoss;
            var distance = lookAt.magnitude;

            if (distance < m_distanceKnockback)
            {
                var bonusDistance = (m_distanceKnockback - distance) * m_weidhtKnockback;
                Vector3 targetKnocback = posBoss + lookAt.normalized * (m_distanceKnockback + bonusDistance);
                targetKnocback.z = hero.transform.position.z;

                DOTween.To(() => hero.transform.position, _pos => hero.rig.MovePosition(_pos), targetKnocback, 0.2f).SetUpdate(UpdateType.Fixed);
            }
        }
    }

#if SERVICE_DEV
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            bool isBuffActive = boss.buff.IsActive(BuffType.BUFF_NO_TAKEN_DAMAGE);
            if (isBuffActive)
                boss.buff.Remove(BuffType.BUFF_NO_TAKEN_DAMAGE);

            boss.OnDamage(null, boss.stat.healthMax * .25f);

            if (isBuffActive)
                boss.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            if (DataManager.bossRaid.raidStatus == BossRaidStatusType.FirstPhase ||
                DataManager.bossRaid.raidStatus == BossRaidStatusType.SecondPhase)
            {
                if (boss.buff.IsActive(BuffType.BUFF_NO_TAKEN_DAMAGE))
                {
                    TeamManager.instance.RemoveBuff(BuffType.BUFF_NO_TAKEN_DAMAGE);
                    boss.buff.Remove(BuffType.BUFF_NO_TAKEN_DAMAGE);
                }
                else
                {
                    TeamManager.instance.AddBuff(BuffType.BUFF_NO_TAKEN_DAMAGE);
                    boss.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);
                }
            }
        }
    }
#endif

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Character_Enemy_RaidBoss boss;
        public Character_Enemy_RaidBoss bossJIN;

        public GameObject fxChangeBoss;

        public void Initialize(Transform _transform)
        {
            var pBoss = _transform.Find("Boss");
            boss = pBoss.GetChild(0).GetComponent<Character_Enemy_RaidBoss>();
            bossJIN = pBoss.GetChild(1).GetComponent<Character_Enemy_RaidBoss>();

            fxChangeBoss = pBoss.Find("FX_BossChange").gameObject;
        }
    }
    #endregion VALIDATE

}
