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
        transform.GetComponent<Button>("Dimm")?.onClick.AddListener(Close);
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
        // 가능 모든 보상
        {
            List<TableCastleMissionRewardData> dbFixed = new();

            for (int i = 0; i < _missionDatas.Length; i++)
                dbFixed.AddRange(TableManager.castleMissonReward.GetReward(_missionDatas[i]).Where(x => x.unlock_pct > 0 && x.unlock_pct <= _missionDatas[i].percentStat).ToList());
            dbFixed = dbFixed.SortByDescending(x => x.unlock_pct);

            m_element.reward.SetReward_ResultRandom(dbFixed.ToArray());
        }

        await UniTask.WaitUntil(() => resultType != StatusType.Wait);

        await Utils.SetActivePunchAsync(m_element.panel, false, true);

        gameObject.SetActive(false);
    }

    //void ActionReward(params Data_Castle_Mission.CastleMissionData[] _missionDatas)
    //{
    //    // 보상 연출 해주자
    //    //Dictionary<ItemType, TableItemData> dbRewards = new();
    //    var rewards = new List<RewardWorker.RewardItemData>();
    //    foreach (var m in _missionDatas)
    //    {
    //        var reward = TableManager.castleMissonReward.GetReward(m).Where(x => x.unlock_pct <= m.percentStat).ToList();
    //        foreach (var r in reward)
    //            rewards.Add(new(r.reward_key, Random.Range(r.reward_min, r.reward_max + 1)));
    //    }

    //    var totalGold = rewards.FindAll(x => x.itemType == ItemType.Gold).Sum(x => x.count);
    //    var totalRice = rewards.FindAll(x => x.itemType == ItemType.Rice).Sum(x => x.count);
    //    DataManager.userInfo.AddAsset(totalGold, totalRice, false, false);

    //    foreach (var r in rewards)
    //        RewardWorker.instance.Run(CameraManager.posPointer, r.itemType, r.count, _isPopup: true, _durationWait: Random.Range(0.1f, .5f));
    //}

    async UniTask OnButtonAsync_Confirm()
    {
        var rewards = await DataManager.castle.mission.CompleteMissionAsync((_result, _exp) =>
        {
            resultType = _result;
        }, m_missionDatas);

        if (resultType == StatusType.Success)
        {
            RewardWorker.OpenRewardPopup(rewards.ToArray());
        }
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
