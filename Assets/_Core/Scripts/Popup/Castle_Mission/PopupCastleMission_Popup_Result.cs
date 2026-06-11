using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PopupCastleMission_Popup_Result : PopupCastleMission_Popup_Info
{
    protected PopupCastleMission_Popup_Result() : base() { }

    [SerializeField] ScrollRect m_scrollBatchHero;

    Data_Castle_Mission.CastleMissionData[] m_missionDatas;

    protected override void Awake()
    {
        transform.GetComponent<Button>("Panel/btn_close")?.onClick.AddListener(Close);
        m_element.btnStart.onClick.AddListener(() => OnButtonAsync_Confirm().Forget());
    }

    public async UniTask OpenAsync(params Data_Castle_Mission.CastleMissionData[] _missionDatas)
    {
        m_missionDatas = _missionDatas;

        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);

        resultType = StatusType.Wait;

        var firstMission = _missionDatas.First();

        m_element.txtTitle.text = $"임무_결과_:_[{(_missionDatas.Length > 1 ? "전체" : TableManager.stringTable.GetGradeType(firstMission.grade))}]";

        m_element.txtName.text = firstMission.missionNameStat;

        if (_missionDatas.Length > 1)
            m_element.txtName.text += $"_외_{_missionDatas.Length - 1}건";

        // 관아 레벨
        {
            var levelInfo = DataManager.castle.mission.levelInfo;
            var addExp = _missionDatas.Sum(x => x.dbGradeData.missionXp);
            m_element.txtContent_Exp.text = $"획득_경험치 : +{addExp.AmountKMBT(_isMBT: true)}";
            m_element.gauge.textTitle = $"Lv.{levelInfo.level}_관아_경험치 : ";
            m_element.gauge.textAmount = $"{levelInfo.nowExp + addExp:#,0} / {levelInfo.maxExp:#,0}";
            m_element.gauge.fillAmount = levelInfo.nowExp / (float)levelInfo.maxExp;
        }

        m_element.reward.SetTitleResult();

        // 영웅 목록
        {
            //일단 완료한 모든 인원을 가져와야 해.
            var keyHero = _missionDatas.Select(x => x.heroes).ToList();
            var myHeroes = DataManager.userInfo.myHero.Where(x => keyHero.Any(k => k.Contains(x.key))).ToList();

            var parent = m_element.pHeroIcon;
            int i = 0;

            for (; i < myHeroes.Count; i++)
            {
                var heroData = myHeroes[i];

                bool isNew = i == parent.childCount;
                var item = isNew ? Instantiate(m_element.baseHeroIcon, parent) :
                    parent.GetChild(i).GetComponent<HeroIconComponent>();

                item.gameObject.SetActive(true);
                item.SetHeroData(heroData, null, null, true);
            }

            for (; i < parent.childCount; i++)
                parent.GetChild(i).gameObject.SetActive(false);

            parent.ForceRebuildLayout();
            m_scrollBatchHero.content.anchoredPosition = Vector2.zero;
        }

        // 확정된 모든 보상
        {
            List<TableCastleMissionRewardData> dbFixed = new();

            for (int i = 0; i < _missionDatas.Length; i++)
                dbFixed.AddRange(TableManager.castleMissonReward.GetReward(_missionDatas[i]).Where(x => x.unlock_pct == 0).ToList());

            m_element.reward.SetReward_ResultFixed(dbFixed.ToArray());
        }

        await UniTask.WaitUntil(() => resultType != StatusType.Wait);

        await Utils.SetActivePunchAsync(m_element.panel, false, true);

        gameObject.SetActive(false);
    }

    void ActionReward(params Data_Castle_Mission.CastleMissionData[] _missionDatas)
    {
        // 보상 연출 해주자
        //Dictionary<ItemType, TableItemData> dbRewards = new();
        var rewards = new List<RewardWorker.RewardItemData>();
        foreach (var m in _missionDatas)
        {
            var reward = TableManager.castleMissonReward.GetReward(m).Where(x => x.unlock_pct <= m.percentStat).ToList();
            foreach (var r in reward)
            {
                rewards.Add(new()
                {
                    itemType = r.reward_key,
                    count = UnityEngine.Random.Range(r.reward_min, r.reward_max + 1)
                });
                //if (dbRewards.ContainsKey(r.reward_key))
                //{
                //    var db = dbRewards[r.reward_key];
                //    db.count += UnityEngine.Random.Range(r.reward_min, r.reward_max + 1);
                //}
                //else
                //{
                //    dbRewards.Add(r.reward_key, new()
                //    {
                //        key = r.reward_key,
                //        value = r.reward_value,
                //        count = UnityEngine.Random.Range(r.reward_min, r.reward_max + 1)
                //    });
                //}
            }
        }

        //var rewards = dbRewards.Values.Select(x => new RewardWorker.RewardItemData(x.key, x.count)).ToList();

        var totalGold = rewards.Where(x => x.itemType == ItemType.Gold).Sum(x => x.count);
        var totalRice = rewards.Where(x => x.itemType == ItemType.Rice).Sum(x => x.count);
        DataManager.userInfo.AddAsset(totalGold, totalRice, false, false);

        foreach (var r in rewards)
            RewardWorker.instance.Run(CameraManager.posPointer, r.itemType, r.count, _isPopup: true, _durationWait: Random.Range(0.1f, .5f));
    }

    async UniTask OnButtonAsync_Confirm()
    {
        await DataManager.castle.mission.CompleteMissionAsync((_result, _exp) =>
        {
            resultType = _result;

            if (resultType == StatusType.Success)
                ActionReward(m_missionDatas);
        }, m_missionDatas);
    }

    public override bool CloseEscape()
    {
        if (gameObject.activeSelf == false)
            return true;

        Close();

        return false;
    }
    public override void Close()
    {
        if (resultType == StatusType.Wait)
            resultType = StatusType.Cancel;
    }

    public override void OnManualValidate()
    {
        base.OnManualValidate();

        m_scrollBatchHero = transform.GetComponent<ScrollRect>("Panel/Info");
    }
}
