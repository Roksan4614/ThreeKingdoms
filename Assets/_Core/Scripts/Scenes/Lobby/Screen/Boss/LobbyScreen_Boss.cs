using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Boss : LobbyScreen_Base
{
    Dictionary<WeekdayType, LobbyScreen_Boss_Tab_Slot> m_dicTabSlot = new();
    List<ItemComponent> m_rewards = new();

    WeekdayType m_curWeekday = WeekdayType.None;

    private void Start()
    {
        m_element.btnStart.onClick.AddListener(() => EnterAsync(false).Forget());
        m_element.btnSweep.onClick.AddListener(() => EnterAsync(true).Forget());
        m_element.btnAD.onClick.AddListener(() => ShowAdsAsync().Forget());

        SetCountText();
        SlotDayChange();

        Signal.instance.DayChange.connect = SlotDayChange;
    }

    protected override bool IsEscapeloseScreen()
    {
        return PopupManager.instance.IsOpenPopup(PopupType.DailyDungeonResult) == false;
    }

    bool m_isRunningEnter = false;
    async UniTask EnterAsync(bool _isSweep)
    {
        if (m_isRunningEnter == true)
            return;

        m_isRunningEnter = true;

        if (DataManager.dailyDungeon.data.count <= 0)
        {
            var result = await PopupManager.instance.OpenModalAsync("광고보기??_");

            if (result == StatusType.Success && await ShowAdsAsync() == false)
                PopupManager.instance.AlertShow("입장할_수_없습니다.");
        }
        else if (_isSweep)
            await DataManager.dailyDungeon.SweepAsync(m_curWeekday, SetCountText);
        else
            await DataManager.dailyDungeon.EnterAsync(m_curWeekday);

        m_isRunningEnter = false;
    }

    async UniTask<bool> ShowAdsAsync()
    {
        if (await DataManager.dailyDungeon.ShowAdsAsync() == true)
        {
            SetCountText();
            return true;
        }

        return false;
    }

    void SetCountText()
    {
        m_element.txtCount.text = $"일일_입장_가능_횟수: {DataManager.dailyDungeon.data.count}";
        m_element.btnAD.text = $"{DataManager.dailyDungeon.data.adCount}/3";
    }

    public override void Open(LobbyScreenType _prevScreen)
    {
        base.Open(_prevScreen);

        if (m_curWeekday == WeekdayType.None)
            m_curWeekday = (WeekdayType)Utils.GetUTC().DayOfWeek;
    }

    public override void Close(bool _isTween = true)
    {
        base.Close(_isTween);
    }

    void OnButton_Tab(TableDailyDungeonBossData _bossData, bool _isForce = false)
    {
        if (m_curWeekday == _bossData.weekday && _isForce == false)
            return;

        if (m_dicTabSlot.ContainsKey(m_curWeekday))
            m_dicTabSlot[m_curWeekday].SetSelect(false);

        m_curWeekday = _bossData.weekday;
        m_dicTabSlot[m_curWeekday].SetSelect(true);

        SetDungeonInfo(_bossData);

        var gradeType = DataManager.dailyDungeon.GetRecordGradeType(m_curWeekday).gradeType;
        if (gradeType > GradeType.Normal != m_element.btnSweep.gameObject.activeSelf)
            m_element.btnSweep.gameObject.SetActive(gradeType > GradeType.Normal);

        // 탭 현재 위치로
        int idxWeekday = (int)m_curWeekday;
        var layout = m_element.scrollTab.content.GetComponent<HorizontalLayoutGroup>();
        var widthSlot = ((RectTransform)m_element.scrollTab.content.GetChild(0)).rect.width;
        var posX = m_element.scrollTab.viewport.rect.width * 0.5f - widthSlot * idxWeekday - layout.spacing * idxWeekday + widthSlot * .5f;
        m_element.scrollTab.content.SetAnchoredPositionX(Mathf.Min(0, posX));
        m_element.scrollTab.velocity = Vector2.zero;
    }

    void SetDungeonInfo(TableDailyDungeonBossData _bossData)
    {
        m_element.txtDesc.text = _bossData.desc;
        m_element.txtName.text = _bossData.name;
        m_element.txtClass.text = _bossData.className;

        var recordData = DataManager.dailyDungeon.GetRecordGradeType(_bossData.weekday);

        bool isHasRecord = recordData.gradeType > GradeType.Normal || recordData.percent > 0;
        m_element.txtRecord.text = $"최고_기록: [{(isHasRecord ? TableManager.stringTable.GetGradeType(recordData.gradeType) : "없음_")}]";
        if (isHasRecord)
            m_element.txtRecord.text += $"<size=90%><color=#555555> ({(recordData.percent * 100):0.#0}%)</color></size>";
        SetRewardData(_bossData);

        // BG
        for (int i = 0; i < m_element.parentBG.childCount; i++)
            m_element.parentBG.GetChild(i).gameObject.SetActive(i == (int)_bossData.weekday - 1);

    }

    void SetRewardData(TableDailyDungeonBossData _bossData)
    {
        var rewardData = TableManager.dailyDungeonGrade.list.SortByDescending(x => (int)x.dungeon_boss_grade)[0];

        List<TableItemData> tableItem = rewardData.GetReward(_bossData.dungeon_boss_class, false);

        var parent = m_element.reward;
        int i = 0;
        for (; i < tableItem.Count; i++)
        {
            var slot = (i == parent.childCount ? Instantiate(parent.GetChild(0), parent) : parent.GetChild(i))
                .GetComponent<ItemComponent>();

            slot.SetItemData(tableItem[i]);
        }

        for (; i < parent.childCount; i++)
            parent.GetChild(i).gameObject.SetActive(false);
    }

    void SlotDayChange()
    {
        var weekday = (WeekdayType)Utils.GetUTC().DayOfWeek;
        if (weekday == WeekdayType.Sunday)
            weekday = WeekdayType.Monday;

        SetTab(weekday);
        OnButton_Tab(TableManager.dailyDungeonBoss.Get(weekday), true);
    }

    void SetTab(WeekdayType _weekday)
    {
        if (gameObject.activeInHierarchy == false)
            return;

        var weekday = (WeekdayType)Utils.GetUTC().DayOfWeek;
        var dbDungeon = TableManager.dailyDungeonBoss.list.SortBy(x => (int)x.weekday);
        var content = m_element.scrollTab.content;
        for (int i = 0; i < dbDungeon.Count; i++)
        {
            var slot = (i == content.childCount ? Instantiate(content.GetChild(0), content) : content.GetChild(i))
                .GetComponent<LobbyScreen_Boss_Tab_Slot>();

            slot.SetDungeonData(weekday, dbDungeon[i], _bossData => OnButton_Tab(_bossData));

            m_dicTabSlot.Add(dbDungeon[i].weekday, slot);
        }
    }

    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_element.Initialize(transform);
    }


    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ScrollRect scrollTab;

        public TextMeshProUGUI txtDesc;
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtClass;
        public TextMeshProUGUI txtRecord;
        public TextMeshProUGUI txtCount;

        public Transform reward;

        public ButtonHelper btnStart;
        public ButtonHelper btnSweep;
        public ButtonHelper btnAD;

        public GameObject[] objBG;

        public void Initialize(Transform _transform)
        {
            scrollTab = _transform.GetComponent<ScrollRect>("Panel/Front/Tab");

            var info = _transform.Find("Panel/Front/Info");
            txtDesc = info.GetComponent<TextMeshProUGUI>("txt_desc");
            txtName = info.GetComponent<TextMeshProUGUI>("txt_name");
            txtClass = info.GetComponent<TextMeshProUGUI>("txt_class");
            txtRecord = info.GetComponent<TextMeshProUGUI>("txt_record");

            reward = _transform.Find("Panel/Front/Reward/Panel");

            btnStart = _transform.GetComponent<ButtonHelper>("Panel/Front/Button/btn_start");
            btnSweep = _transform.GetComponent<ButtonHelper>("Panel/Front/Button/btn_sweep");
            btnAD = _transform.GetComponent<ButtonHelper>("Panel/Front/Button/btn_ad");

            txtCount = _transform.GetComponent<TextMeshProUGUI>("Panel/Front/Button/txt_count");

            var bg = _transform.Find("Panel/BG");
            List<GameObject> lstBG = new();
            for (int i = 0; i < bg.childCount; i++)
                lstBG.Add(bg.GetChild(i).gameObject);
            objBG = lstBG.ToArray();
        }

        public Transform parentBG => objBG[0].transform.parent;
    }
}

//CameraManager.instance.SetAddPosY(-2, 20);
//CameraManager.instance.SetAddPosY(0, 20);