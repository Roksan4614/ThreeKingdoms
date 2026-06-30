using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Boss : LobbyScreen_Base
{
    List<ItemComponent> m_rewards = new();

    private void Start()
    {
        // Tab 持失馬切
        SetTab();

        Signal.instance.DayChange.connect = SlotDayChange;
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

        public Transform reward;

        public void Initialize(Transform _transform)
        {
            scrollTab = _transform.GetComponent<ScrollRect>("Panel/Front/Tab");

            var info = _transform.Find("Panel/Front/Info");
            txtDesc = info.GetComponent<TextMeshProUGUI>("txt_desc");
            txtName = info.GetComponent<TextMeshProUGUI>("txt_name");
            txtClass = info.GetComponent<TextMeshProUGUI>("txt_class");
            txtRecord = info.GetComponent<TextMeshProUGUI>("txt_record");

            reward = _transform.Find("Panel/Front/Reward/Panel");
        }
    }
}

//CameraManager.instance.SetAddPosY(-2, 20);
//CameraManager.instance.SetAddPosY(0, 20);