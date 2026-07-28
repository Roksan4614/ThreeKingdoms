using Cysharp.Threading.Tasks;
using UnityEngine;

public class DailyDungeonComponent : MonoBehaviour, IValidatable
{
    public Character_Enemy_DailyDungeonBoss boss => m_element.boss;

    void Start()
    {
        m_element.boss.SetBossData(m_element.boss.name);
        m_element.boss.buff.Add(BuffType.BUFF_NO_DIE);

        StageManager.instance.ClearEnemyList();
        StageManager.instance.AddEnemyList(m_element.boss);
    }

    private void Update()
    {
#if SERVICE_DEV
        if (Input.GetKeyDown(KeyCode.Escape))
            DataManager.dailyDungeon.ExitAsync().Forget();
#endif
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Character_Enemy_DailyDungeonBoss boss;

        public GameObject fxChangeBoss;

        public void Initialize(Transform _transform)
        {
            var pBoss = _transform.Find("Boss");
            boss = pBoss.GetChild(0).GetComponent<Character_Enemy_DailyDungeonBoss>();
        }
    }
    #endregion VALIDATE
}
