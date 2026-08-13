using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using System.Linq;
using UnityEngine;

public class PopupTournament_Batch_Relic : LobbyScreen_Hero_Relic
{
    ButtonHelper m_btnAttack;
    ButtonHelper m_btnDefence;

    bool m_isAttackType;

    public bool isNeedUpdateClose { get; set; }

    protected override void Awake()
    {
        m_isScreenHeroMode = false;
        base.Awake();

        m_btnAttack = transform.GetComponent<ButtonHelper>("Tab_Type/btn_attack");
        m_btnDefence = transform.GetComponent<ButtonHelper>("Tab_Type/btn_defence");

        m_btnAttack.onClick.AddListener(() => OnButton_Type(true));
        m_btnDefence.onClick.AddListener(() => OnButton_Type(false));

        m_isAttackType = TournamentWorker.instance.isAttackType;
        m_btnAttack.SetDrawSelect(m_isAttackType == true);
        m_btnDefence.SetDrawSelect(m_isAttackType == false);
    }

    void OnButton_Type(bool _isAttack)
    {
        if (_isAttack != m_isAttackType)
        {
            TournamentWorker.instance.isAttackType = _isAttack;
            UpdateTreasure_TotalStat();
        }
    }

    protected override void SetActiveTab(TabType _tabType)
    {
        base.SetActiveTab(_tabType);

        m_btnAttack.transform.parent.gameObject.SetActive(_tabType == TabType.Treasure);
    }

    protected override void UpdateTreasure_TotalStat(bool _isOnClick = false)
    {
        if (_isOnClick && TournamentWorker.instance.isAttackType == true)
            isNeedUpdateClose = true;

        var scroll = m_element.scroll;

        m_isAttackType = TournamentWorker.instance.isAttackType;
        m_btnAttack.SetDrawSelect(m_isAttackType == true);
        m_btnDefence.SetDrawSelect(m_isAttackType == false);

        var batchData = TournamentWorker.instance.GetBatchData(m_isAttackType);

        var dbTreasure = TableManager.treasure.list.Where(x => x.isActive)
            .OrderByDescending(x => batchData.treasure.Find(y => y.key == x.key).isBatch)
            .ThenBy(x => batchData.treasure.Find(y => y.key == x.key).tickBatch)
            .ToArray();

        m_element.scroll.Initialize<PopupTournament_Batch_Relic_Item>(dbTreasure.Length,
            (_item, _idxData) =>
            {
                _item.SetTreasureDataAsync(batchData.treasure, dbTreasure[_idxData]
                    , _heroInfo => OnButton_Item(_heroInfo.key.IsActive() ? TabType.Relic : TabType.Treasure, _heroInfo)).Forget();
#if UNITY_EDITOR
                _item.name = dbTreasure[_idxData].key;
#endif
            });

        m_element.pTotalClass.gameObject.SetActive(false);
        m_element.pTotalTreasure.gameObject.SetActive(true);
        m_element.txtTreasureCount.gameObject.SetActive(true);

        SetTextTotalTreasure();
    }
}
