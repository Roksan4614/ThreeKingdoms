using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Boss : LobbyScreen_Base
{
    List<ItemComponent> m_rewards = new();

    WeekdayType m_curWeekday = WeekdayType.None;

    private void Start()
    {
        m_element.btnStart.onClick.AddListener(() => EnterAsync().Forget());
        m_element.btnAD.onClick.AddListener(() => ShowAdsAsync().Forget());

        SetCount();
        // Tab 생성하자
        SetTab();

        Signal.instance.DayChange.connect = SlotDayChange;
    }

    async UniTask EnterAsync()
    {
        if (DataManager.dailyDungeon.data.count <= 0)
        {
            if (await ShowAdsAsync() == false)
                PopupManager.instance.AlertShow("더 이상 입장할 수 없습니다.");

            return;
        }

        await DataManager.dailyDungeon.EnterAsync(m_curWeekday);
    }

    async UniTask<bool> ShowAdsAsync()
    {
        if (await DataManager.dailyDungeon.ShowAdsAsync() == true)
        {
            SetCount();
            return true;
        }

        return false;
    }

    void SetCount()
    {
        m_element.txtCount.text = $"일일_입장_가능_횟수: {DataManager.dailyDungeon.data.count}";
        m_element.btnAD.text = $"{DataManager.dailyDungeon.data.adCount}/3";
    }

    public override void Open(LobbyScreenType _prevScreen)
    {
        base.Open(_prevScreen);

        OnButton_Tab(TableManager.dailyDungeonBoss.Get((WeekdayType)Utils.GetUTC().DayOfWeek));
    }

    public override void Close(bool _isTween = true)
    {
        base.Close(_isTween);
    }

    void OnButton_Tab(TableDailyDungeonBossData _bossData)
    {
        if (m_curWeekday == _bossData.weekday)
            return;

        m_curWeekday = _bossData.weekday;
        SetDungeonInfo(_bossData);
    }

    void SetDungeonInfo(TableDailyDungeonBossData _bossData)
    {
        m_element.txtDesc.text = _bossData.desc;
        m_element.txtName.text = _bossData.name;
        m_element.txtClass.text = _bossData.className;

        SetRewardData(_bossData);
    }

    void SetRewardData(TableDailyDungeonBossData _bossData)
    {
        var rewardData = TableManager.dailyDungeonGrade.list.SortByDescending(x => (int)x.dungeon_boss_grade)[0];

        List<TableItemData> tableItem = new();
        tableItem.Add(new()
        {
            key = ItemType.Rice,
        });
        tableItem.Add(new()
        {
            key = ItemType.Gold,
        });
        tableItem.Add(new()
        {
            category = ItemCategoryType.Soul_Stone,
            key = ItemType.Class_Soul_Stone,
            value = _bossData.dungeon_boss_class.ToString()
        });
        tableItem.Add(new()
        {
            key = ItemType.Time_Stone,
        });

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
        SetTab();
        OnButton_Tab(TableManager.dailyDungeonBoss.Get((WeekdayType)Utils.GetUTC().DayOfWeek));
    }

    void SetTab()
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

            slot.SetDungeonData(weekday, dbDungeon[i], OnButton_Tab);
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
        public ButtonHelper btnAD;

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
            btnAD = _transform.GetComponent<ButtonHelper>("Panel/Front/Button/btn_ad");

            txtCount = _transform.GetComponent<TextMeshProUGUI>("Panel/Front/Button/txt_count");
        }
    }
}

//CameraManager.instance.SetAddPosY(-2, 20);
//CameraManager.instance.SetAddPosY(0, 20);