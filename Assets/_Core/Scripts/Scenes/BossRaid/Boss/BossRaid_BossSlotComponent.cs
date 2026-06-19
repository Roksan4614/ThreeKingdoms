using UnityEngine;
using static Data_BossRaid;

public class BossRaid_BossSlotComponent : MonoBehaviour, IValidatable
{
    public Character_Enemy_RaidBoss boss =>
        DataManager.bossRaid.raidStatus == BossRaidStatusType.SecondPhase ? m_element.bossJIN : m_element.boss;

    protected virtual void Awake()
    {
#if UNITY_EDITOR
        if (Configure.instance.isBooted == false)
            return;
#endif

        m_element.boss.gameObject.SetActive(DataManager.bossRaid.raidStatus != BossRaidStatusType.SecondPhase);
        m_element.bossJIN.gameObject.SetActive(DataManager.bossRaid.raidStatus == BossRaidStatusType.SecondPhase);

        boss.SetBossData(DataManager.bossRaid.data.keyBoss);

        Signal.instance.BossRaidStatus.connect = SlotBossRaidStatus;
    }

    void SlotBossRaidStatus(BossRaidStatusType _status)
    {
        if (_status == BossRaidStatusType.Finish_FirstPhase)
        {
            Utils.AfterSecond(() =>
            {
                m_element.fxChangeBoss.transform.position = m_element.bossJIN.transform.position = m_element.boss.transform.position;

                m_element.fxChangeBoss.SetActive(true);
                Utils.AfterSecond(() =>
                {
                    m_element.boss.gameObject.SetActive(false);
                    m_element.bossJIN.gameObject.SetActive(true);

                    m_element.bossJIN.anim.Play("Boss_Die_End");

                }, 1.5f);
            }, 3f);
        }
        else if (_status == BossRaidStatusType.SecondPhase)
        {
        }
    }

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
