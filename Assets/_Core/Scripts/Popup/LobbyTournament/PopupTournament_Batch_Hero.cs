using Cysharp.Threading.Tasks;
using DG.Tweening;
using Rev9.Tournament;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupTournament_Batch_Hero : LobbyScreen_Hero_Hero
{
    bool m_isAttackType;
    public bool isUpdated { get; private set; }

    struct ButtonActionData
    {
        public ButtonHelper btnSlot;
        public ButtonHelper btnAction;

        public RectTransform rtPanel => (RectTransform)btnAction.transform.parent;
    }
    ButtonActionData[] m_batchAction;

    protected override void Awake()
    {
        base.Awake();

        var pAction = transform.Find("Batch_Action");
        m_batchAction = new ButtonActionData[9];
        for (int i = 0; i < m_batchAction.Length; i++)
        {
            var idx = i;
            m_batchAction[i].btnSlot = (i == pAction.childCount ? Instantiate(pAction.GetChild(0), pAction) : pAction.GetChild(i)).GetComponent<ButtonHelper>();

            var slot = m_batchAction[i].btnSlot;
            slot.name = "Slot_" + i;

            m_batchAction[i].btnAction = slot.transform.GetComponent<ButtonHelper>("Panel/btn_action");
            m_batchAction[i].btnAction.gameObject.SetActive(false);

            slot.funcDown = _pointerId => ActionAsync_Down(idx, _pointerId).Forget();
            //slot.funcUp = () => ActionAsync_Up(idx).Forget();
            slot.funcEnter = () => Action_Enter(idx);
            slot.funcExit = () => Action_Exit(idx);
        }
    }

    protected override void SetFilterSize()
    {
        var offsetMax = m_popupFilter.rtPanel.offsetMax;
        offsetMax.y = -810;
        m_popupFilter.SetFilterSize(offsetMax);
    }

    protected override void Start()
    {
        InstantiateList();
        m_isStarted = true;

        m_isAttackType = false;
        OnButtonAsync_Type(true).Forget();

        m_elementTournament.btnAttack.onClick.AddListener(() => OnButtonAsync_Type(true).Forget());
        m_elementTournament.btnDefence.onClick.AddListener(() => OnButtonAsync_Type(false).Forget());

        var parentBatchAction = m_batchAction[0].btnSlot.transform.parent;
        parentBatchAction.ForceRebuildLayout();
        parentBatchAction.GetComponent<GridLayoutGroup>().enabled = false;
        var width = m_batchAction[0].rtPanel.rect.width * 0.05f;
        for (int i = 0; i < 3; i++)
        {
            var rt = m_batchAction[i * 3].btnSlot.rt;
            rt.SetAnchoredPositionX(rt.anchoredPosition.x + width);

            rt = m_batchAction[i * 3 + 2].btnSlot.rt;
            rt.SetAnchoredPositionX(rt.anchoredPosition.x - width);
        }

    }

    protected override void OnEnable()
    {
        if (m_isStarted == true)
        {
            //if (m_isNeedUpdateLayout)
            //    TournamentWorker.instance.UpdateHero();

            OnButtonAsync_Type(true, m_isNeedUpdateLayout).Forget();
        }

        isUpdated = false;
    }

    public async UniTask CloseAsync(UnityAction _callback)
    {
        if (isUpdated == true)
            await TournamentWorker.instance.API_UpdateTeamData(m_isAttackType, m_batchData);
        _callback();
    }

    async UniTask OnButtonAsync_Type(bool _isAttackType, bool _isForce = false)
    {
        if (m_isAttackType == _isAttackType && _isForce == false)
            return;

        if (isUpdated == true)
        {
            isUpdated = false;
            await TournamentWorker.instance.API_UpdateTeamData(m_isAttackType, m_batchData);
        }

        m_isAttackType = TournamentWorker.instance.isAttackType = _isAttackType;

        m_batchData = TournamentWorker.instance.GetBatchData(m_isAttackType);

        m_elementTournament.btnAttack.SetDrawSelect(_isAttackType == true);
        m_elementTournament.btnDefence.SetDrawSelect(_isAttackType == false);

        m_myHero.Clear();
        m_myHero.AddRange(m_batchData.heroes);

        m_elementTournament.panelBatch.SetBatchDataAsync(m_batchData).Forget();

        SetLayout_List();
    }

    #region ACTION_DOWN
    struct ActionDownData
    {
        public int idxEnter;
        public Vector3 prevPos;
    }
    ActionDownData m_actionData;

    void Action_Enter(int _idx)
    {
        m_actionData.idxEnter = _idx;
        m_batchAction[_idx].rtPanel.gameObject.SetActive(true);
    }
    void Action_Exit(int _idx)
    {
        m_batchAction[_idx].rtPanel.gameObject.SetActive(false);
        if (m_actionData.idxEnter == _idx)
            m_actionData.idxEnter = -1;
    }
    async UniTask ActionAsync_Down(int _idx, int _pointerId)
    {
        var hero = m_elementTournament.panelBatch.GetCharacter(_idx);

        if (hero == null)
            return;

        if (Configure.isPC == false)
            Action_Enter(m_actionData.idxEnter);
        else if (ControllerManager.instance.isRightClick)
        {
            OnRightClick_List(m_itemList.Find(x => hero.info.skin == x.data.skin));
            return;
        }

        var shadow = hero.element.panel.Find("Shadow");
        shadow.localPosition += Vector3.down * .3f;

        var gap = hero.transform.position - CameraManager.GetPosPointer(_pointerId) + Vector3.up * .3f;
        await hero.transform.DOMove(CameraManager.GetPosPointer(_pointerId) + gap, .05f);

        while (ControllerManager.isClick)
        {
            hero.transform.position = CameraManager.GetPosPointer(_pointerId) + gap;
            await UniTask.NextFrame(destroyCancellationToken);
        }

        // 다른 곳으로 끌었다면..
        if (_idx != m_actionData.idxEnter && m_actionData.idxEnter >= 0)
        {
            // 그 안에 이미 다른 애가 있어?
            var prevHero = m_elementTournament.panelBatch.GetCharacter(m_actionData.idxEnter);
            if (prevHero != null)
            {
                prevHero.transform.SetParent(m_elementTournament.panelBatch.GetSlot(_idx));
                prevHero.transform.DOLocalMove(Vector3.zero, 0.05f).Forget();
            }

            hero.transform.SetParent(m_elementTournament.panelBatch.GetSlot(m_actionData.idxEnter));

            m_batchData = TournamentWorker.instance.ChangePosition(m_batchData, _idx, m_actionData.idxEnter); ;

            SetLayout_List();

            if (m_isAttackType == true)
                isUpdated = true;
        }

        hero.transform.DOLocalMove(Vector3.zero, 0.05f).Forget();
        shadow.DOLocalMove(Vector3.zero, 0.05f).Forget();

        if (Configure.isPC == false)
            Action_Exit(m_actionData.idxEnter);
    }
    #endregion ACTION_DATA

    TournamentBatchData m_batchData;

    protected override void OnRightClick_List(HeroIconComponent _item)
    {
        var teamData = m_isAttackType ? TournamentWorker.data.teamAttack : TournamentWorker.data.teamDefence;

        ResetActiveButton_List();
        var itemData = _item.data;

        // 이미 출진 중이라면?
        if (itemData.isBatch == true)
        {
            if (m_batchData.heroes.Count == 1)
            {
                PopupManager.instance.AlertShow("최소_1명은_배치해야_합니다.");
                return;
            }

            itemData.isBatch = false;
            _item.UpdateHeroInfo(itemData);

            var idx = m_batchData.heroes.FindIndex(x => x.skin == _item.data.skin);
            m_batchData.heroes.RemoveAt(idx);
        }
        // 새로 출진하는 거라면
        else
        {
            if (m_batchData.heroes.Count == 4)
            {
                PopupManager.instance.AlertShow("최대_4명까지_배치할_수_있습니다.");
                return;
            }

            itemData.isBatch = true;
            itemData.sortIdx = TournamentWorker.instance.GetPositionByClass(m_batchData, itemData.classType);
            _item.UpdateHeroInfo(itemData);

            m_batchData.heroes.Add(itemData);
        }

        m_batchData.heroes = m_batchData.heroes.SortBy(x => x.sortIdx);
        m_elementTournament.panelBatch.SetBatchDataAsync(m_batchData).Forget();
        SetLayout_List();
        isUpdated = true;
        return;
    }

    protected override void SetLayout_List(HeroInfoData _updateInfoData = default)
    {
        if (m_isStarted == false)
            return;

        // 업데이트 정보가 필요하다면
        if (_updateInfoData.isActive)
        {
            var indexList = m_itemList.FindIndex(x => x.data.key == _updateInfoData.key);
            if (indexList > -1)
                m_itemList[indexList].UpdateHeroInfo(_updateInfoData);
        }
        List<HeroInfoData> lstHero = new();
        lstHero.AddRange(m_batchData.heroes);
        lstHero.AddRange(DataManager.userInfo.myHero.Where(my => m_batchData.heroes.Any(x => x.skin == my.skin) == false).ToList());

        // myHero batch 한거 false로 바꿔주자
        for (int i = m_batchData.heroes.Count; i < lstHero.Count; i++)
        {
            var hero = lstHero[i];
            if (hero.isBatch == true)
            {
                int idx = m_itemList.FindIndex(x => x.data.key == hero.key);
                hero.isBatch = false;
                lstHero[i] = hero;

                m_itemList[idx].UpdateHeroInfo(hero);
            }
        }

        var sortData = DataManager.userInfo.GetHeroSortData(lstHero);

        //int i = 0;
        for (int i = 0; i < sortData.Count; i++)
        {
            int idx = m_itemList.FindIndex(x => x.data.key == sortData[i].key);
            if (idx == -1)
                continue;

            if (i < m_batchData.heroes.Count)
                m_itemList[idx].UpdateHeroInfo(m_batchData.heroes[i]);

            m_itemList[idx].transform.SetSiblingIndex(i);
            m_itemList[idx].element.panel.gameObject.SetActive(true);
        }

        var parent = m_element.scroll.content;
        for (int i = 0; i < m_itemList.Count; i++)
        {
            var idx = sortData.FindIndex(x => x.key == m_itemList[i].data.key);
            if (idx == -1)
                m_itemList[i].element.panel.gameObject.SetActive(false);
        }
    }

    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_elementTournament.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    ElementData_Tournament m_elementTournament;

    [System.Serializable]
    struct ElementData_Tournament
    {
        public ButtonHelper btnAttack;
        public ButtonHelper btnDefence;

        public PopupTournament_Batch_Panel panelBatch;

        public void Initialize(Transform _transform)
        {
            btnAttack = _transform.GetComponent<ButtonHelper>("Tab_Type/btn_attack");
            btnDefence = _transform.GetComponent<ButtonHelper>("Tab_Type/btn_defence");

            panelBatch = _transform.GetComponent<PopupTournament_Batch_Panel>("Batch");
        }
    }
}
