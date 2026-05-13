using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using CastleMissionData = Data_Castle_Mission.CastleMissionData;

public class PopupCastleMission_Item : MonoBehaviour, IValidatable
{
    CastleMissionData m_missionData;
    CancellationTokenSource m_cts;

    public UnityAction<CastleMissionData> m_onClick;

    private void Awake()
    {
        m_element.btn_batch.onClick.AddListener(()
            => m_onClick(m_missionData));
        transform.GetComponent<Button>().onClick.AddListener(()
            => m_onClick(m_missionData));
    }

    public void Initalize(UnityAction<CastleMissionData> _onClick)
    {
        m_onClick = _onClick;
    }

    public void SetMissionInfo(CastleMissionData _missionData)
    {
        if (m_missionData.idx == _missionData.idx && m_missionData.tickStart == _missionData.tickStart)
            return;

        m_missionData = _missionData;

        m_element.txt_title.text = _missionData.missionName;
        m_element.txt_exp.text = $"획득_경험치_:_+{_missionData.exp.AmountKMBT()}";

        // 확정아이템
        {
            int i = 0;

            var rewardList = _missionData.rewardList.Take(2).ToList();
            for (; i < rewardList.Count; i++)
            {
                var itemIcon = (i == m_element.parentRewardList.childCount ? Instantiate(m_element.baseItem, m_element.parentRewardList) :
                    m_element.parentRewardList.GetChild(i))
                    .GetComponent<ItemComponent>("ItemIcon");

                itemIcon.transform.parent.gameObject.SetActive(true);
                itemIcon.SetItemData(rewardList[i]);
            }

            for (; i < m_element.parentRewardList.childCount; i++)
                m_element.parentRewardList.GetChild(i).gameObject.SetActive(false);

            m_element.parentRewardList.ForceRebuildLayout();
        }

        // 기타아이템
        {
            int i = 0;

            var rewardList = _missionData.rewardList.Take(5).ToList();
            for (; i + 2 < rewardList.Count; i++)
            {
                var itemIcon = (i == m_element.parentRewardList_ETC.childCount ? Instantiate(m_element.baseItem, m_element.parentRewardList_ETC) :
                    m_element.parentRewardList_ETC.GetChild(i))
                    .GetComponent<ItemComponent>("ItemIcon");

                itemIcon.transform.parent.gameObject.SetActive(true);
                itemIcon.SetItemData(rewardList[i + 2]);
            }

            for (; i < m_element.parentRewardList_ETC.childCount; i++)
                m_element.parentRewardList_ETC.GetChild(i).gameObject.SetActive(false);

            m_element.parentRewardList_ETC.ForceRebuildLayout();
        }

        m_element.parentRewardList.ForceRebuildLayout();

        Release_CTS();
        if (_missionData.tickStart > 0)
        {
            TimerAsync().Forget();
        }
        else
        {
            var size = m_element.btn_batch.rt.sizeDelta;
            size.y = 70;
            m_element.btn_batch.rt.sizeDelta = size;

            m_element.btn_batch.SetDrawSelect(false);
            m_element.btn_batch.text = "장수_편성";
            m_element.btn_batch.interactable = true;
        }

        //m_element.imgOutline.color = Palette.GetGradeOutline(m_missionData.grade);
    }

    async UniTask TimerAsync()
    {
        var size = m_element.btn_batch.rt.sizeDelta;
        size.y = 95;
        m_element.btn_batch.rt.sizeDelta = size;

        m_element.btn_batch.interactable = false;

        m_cts = new();
        var token = m_cts.Token;

        var endTime = new DateTime(m_missionData.tickEnd, DateTimeKind.Utc);
        var ts = endTime - DateTime.UtcNow;

        m_element.btn_batch.text = Utils.MSpace(ts.ToString(@"hh\:mm\:ss"), 21) + "\n<size=90%>시간단축";

        if (ts.TotalSeconds > 0)
        {
            m_element.btn_batch.SetDrawSelect(false);

            var delay = ts.TotalSeconds - (int)ts.TotalSeconds;
            string log = $"{delay}/{(float)delay}";

            await UniTask.WaitForSeconds((float)delay, cancellationToken: token);

            while (ts.TotalSeconds > 0)
            {
                ts = endTime - DateTime.UtcNow;
                m_element.btn_batch.text = Utils.MSpace(ts.ToString(@"hh\:mm\:ss"), 21) + "\n<size=90%>시간단축";
                await UniTask.WaitForSeconds(1f, cancellationToken: token);
            }
        }

        size.y = 70;
        m_element.btn_batch.rt.sizeDelta = size;

        m_element.btn_batch.SetDrawSelect(true);
        m_element.btn_batch.text = "_완료_";
    }

    void Release_CTS()
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txt_title;
        public TextMeshProUGUI txt_exp;

        public ButtonHelper btn_batch;

        public Transform parentRewardList;
        public Transform parentRewardList_ETC;

        public Transform baseItem;
        public Image imgOutline;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");
            txt_title = panel.GetComponent<TextMeshProUGUI>("txt_title");
            txt_exp = panel.GetComponent<TextMeshProUGUI>("txt_reward_exp");
            btn_batch = panel.GetComponent<ButtonHelper>("btn_batch");

            parentRewardList = panel.Find("Reward/List");
            parentRewardList_ETC = panel.Find("Reward/List_ETC");
            baseItem = parentRewardList.GetChild(0);

            imgOutline = _transform.GetComponent<Image>("img_outline");
        }
    }
    #endregion VALIDATE

}

