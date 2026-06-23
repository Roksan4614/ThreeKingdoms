using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Linq;
using UnityEngine;

public class RankBossRaidComponent : Singleton<RankBossRaidComponent>, IValidatable
{
    const float c_fSpeed = 3000;
    float m_startPosY;

    bool m_isDoingHide;
    bool m_isHide;

    RectTransform rt => (RectTransform)transform;
    protected override void OnAwake()
    {
        m_startPosY = rt.anchoredPosition.y;
        base.OnAwake();
    }

    void Start()
    {
        m_element.btnHide.onClick.AddListener(() => OnButtonAsync_Hide().Forget());
        Signal.instance.ActiveHUD.connectLambda = new(this, _isActive => gameObject.SetActive(_isActive));

        UpdateRanker();
        DataManager.bossRaid.TestAddTestUser();
    }

    async UniTask OnButtonAsync_Hide()
    {
        if (m_isDoingHide == true)
            return;

        m_isDoingHide = true;
        m_isHide = !m_isHide;

        m_element.btnHide.text = m_isHide ? "▲" : "▼";

        float fSpeed = m_isHide ? -c_fSpeed : c_fSpeed;
        var height = ((RectTransform)transform).rect.height;
        var posY = m_element.posSlotY.Select(x => x - (m_isHide ? 0 : height)).ToArray();
        var targetPosY = m_element.posSlotY.Select(x => x - (m_isHide ? height : 0)).ToArray();

        if (m_isHide == true)
            targetPosY[0] = targetPosY[1] = targetPosY[2] = m_element.posSlotY[m_element.posSlotY.Length - 1];

        while (true)
        {
            int isCompleteCount = 0;
            for (int i = 0; i < targetPosY.Length; i++)
            {
                posY[i] += fSpeed * Time.deltaTime;

                // 아래로 내려간다.
                if ((m_isHide && posY[i] <= targetPosY[i]) ||
                    (m_isHide == false && posY[i] >= targetPosY[i]))
                    isCompleteCount++;

                var posSlot = m_element.slots[i].rt.anchoredPosition;

                if (m_isHide == true)
                    posSlot.y = Mathf.Max(targetPosY[i], posY[i]);
                // 여는데 내 정보라면
                else if (i <= 2)
                {
                    if (posY[i] < m_element.posSlotY[m_element.posSlotY.Length - 1])
                        posSlot.y = m_element.posSlotY[m_element.posSlotY.Length - 1];
                    else
                        posSlot.y = Mathf.Min(targetPosY[i], posY[i]);
                }
                else
                    posSlot.y = Mathf.Min(targetPosY[i], posY[i]);

                m_element.slots[i].rt.anchoredPosition = posSlot;
            }

            if (isCompleteCount == targetPosY.Length)
                break;

            await UniTask.WaitForEndOfFrame();
        }

        m_isDoingHide = false;
    }

    Tween m_tweenMovePanel;
    public void SetMove_HeroInfoDown(bool _isBottom, bool _isTween = true, float _duration = .2f)
    {
        m_tweenMovePanel?.Kill();
        float target = m_startPosY - (_isBottom ? 240 : 0);
        m_tweenMovePanel = rt.DOAnchorPosY(target, _duration);
    }

    public void UpdateRanker()
    {
        var dbRanker = DataManager.bossRaid.rankNow.SortByDescending(x => x.point);

        if (dbRanker.Count == 0)
            dbRanker.Add(new()
            {
                nickname = DataManager.userInfo.nickname
            });

        int startIndex = -2;
        for (int i = 0; i < dbRanker.Count; i++)
        {
            if (dbRanker[i].uid == DataManager.userInfo.uid){
                startIndex = i - 2;
                break;
            }
        }

        for (int i = 0; i < m_element.slots.Length; i++, startIndex++)
        {
            if (startIndex < 0)
            {
                m_element.slots[i].gameObject.SetActive(false);
                continue;
            }

            m_element.slots[i].gameObject.SetActive(true);

            if (startIndex >= dbRanker.Count)
                m_element.slots[i].SetEmpty();
            else
            {
                m_element.slots[i].SetRankData(dbRanker[startIndex]);
            }
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public RankBossRaid_Slot[] slots;
        public ButtonHelper btnHide;

        public float[] posSlotY;

        public void Initialize(Transform _transform)
        {
            slots = _transform.GetComponentsInChildren<RankBossRaid_Slot>().ToArray();
            btnHide = _transform.GetComponent<ButtonHelper>("btn_hide");

            posSlotY = slots.Select(x => x.rt.anchoredPosition.y).ToArray();
        }
        public RankBossRaid_Slot my => slots[2];
    }
    #endregion VALIDATE

}
