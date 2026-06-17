using UnityEngine;

public class BossRaid_BossSlotComponent : MonoBehaviour, IValidatable
{
    public Character_Enemy_RaidBoss boss =>
        BossRaidWorker.instance.isSecondStep ? m_element.bossJIN : m_element.boss;

    protected virtual void Awake()
    {
        if (Configure.instance.isBooted == false)
            return;

        m_element.boss.gameObject.SetActive(BossRaidWorker.instance.isSecondStep == false);
        m_element.bossJIN.gameObject.SetActive(BossRaidWorker.instance.isSecondStep == true);

        boss.SetBossData(DataManager.bossRaid.data.keyBoss);
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

        public void Initialize(Transform _transform)
        {
            var pBoss = _transform.Find("Boss");
            boss = pBoss.GetChild(0).GetComponent<Character_Enemy_RaidBoss>();
            bossJIN = pBoss.GetChild(1).GetComponent<Character_Enemy_RaidBoss>();
        }
    }
    #endregion VALIDATE

}
