using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PopupCastleMission_Popup_Result : PopupCastleMission_Popup_Info
{
    protected PopupCastleMission_Popup_Result() : base() { }

    protected override void Awake()
    {
        m_element.btnStart.onClick.AddListener(OnButton_Confirm);
    }

    public async UniTask OpenAsync(params Data_Castle_Mission.CastleMissionData[] _missionDatas)
    {
        await DataManager.castle.mission.CompleteMissionAsync((_result, _exp) =>
        {
            resultType = _result;

            if (_result == StatusType.Success)
                m_element.txtContent_Exp.text = $"획득_경험치 : +{_exp.AmountKMBT(_isMBT: true)}";

        }, _missionDatas);

        if (resultType == StatusType.Failed)
        {
            Close();
            return;
        }

        gameObject.SetActive(true);
        Utils.SetActivePunch(m_element.panel, true);

        resultType = StatusType.Wait;

        var firstMission = _missionDatas.First();

        m_element.txtTitle.text = $"임무_결과_:_[{(_missionDatas.Length > 1 ? "전체" :TableManager.stringTable.GetString($"GRADE_{firstMission.grade.ToString().ToUpper()}"))}]";
        m_element.txtName.text = _missionDatas.Length > 1 ? "전체_보상받기" : firstMission.missionNameStat;

        // 관아 레벨
        {
            var levelInfo = DataManager.castle.mission.levelInfo;
            m_element.gauge.textTitle = $"Lv.{levelInfo.level}_관아_경험치 : ";
            m_element.gauge.textAmount = $"{levelInfo.nowExp:#,0} / {levelInfo.maxExp:#,0}";
            m_element.gauge.fillAmount = levelInfo.nowExp / (float)levelInfo.maxExp;
        }

        m_element.reward.SetTitleResult();

        // 확정된 모든 보상
        {
            List<TableCastleMissionRewardData> dbFixed = new();

            for (int i = 0; i < _missionDatas.Length; i++)
                dbFixed.AddRange(TableManager.castleMissonReward.GetReward(_missionDatas[i]).Where(x => x.unlock_pct == 0).ToList());

            m_element.reward.SetReward_ResultFixed(dbFixed.ToArray());
        }

        await UniTask.WaitUntil(() => resultType != StatusType.Wait);

        ActionReward(_missionDatas);

        Close();
    }

    void ActionReward(params Data_Castle_Mission.CastleMissionData[] _missionDatas)
    {
        // 보상 연출 해주자
        Dictionary<ItemType, TableItemData> dbRewards = new();
        foreach (var m in _missionDatas)
        {
            var reward = TableManager.castleMissonReward.GetReward(m).Where(x => x.unlock_pct <= m.percentStat).ToList();
            foreach (var r in reward)
            {
                if (dbRewards.ContainsKey(r.reward_key))
                {
                    var db = dbRewards[r.reward_key];
                    db.count += UnityEngine.Random.Range(r.reward_min, r.reward_max + 1);
                }
                else
                {
                    dbRewards.Add(r.reward_key, new()
                    {
                        key = r.reward_key,
                        value = r.reward_value,
                        count = UnityEngine.Random.Range(r.reward_min, r.reward_max + 1)
                    });
                }
            }
        }

        var rewards = dbRewards.Values.Select(x => new RewardWorker.RewardItemData(x.key, x.count)).ToList();

        var totalGold = rewards.Where(x => x.itemType == ItemType.Gold).Sum(x => x.count);
        var totalRice = rewards.Where(x => x.itemType == ItemType.Rice).Sum(x => x.count);
        DataManager.userInfo.AddAsset(totalGold, totalRice, false, false);

        foreach (var r in rewards)
            RewardWorker.instance.Run(CameraManager.posPointer, r.itemType, r.count, _isPopup: true);
    }

    void OnButton_Confirm()
    {
        resultType = StatusType.Success;
    }
}
