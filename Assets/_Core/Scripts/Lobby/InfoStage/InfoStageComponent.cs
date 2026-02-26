using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoStageComponent : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        m_element.boss.gameObject.SetActive(false);
        m_element.infoStage.gameObject.SetActive(false);
        m_element.btn_challenge.gameObject.SetActive(false);
        m_element.rtIconBoss.gameObject.SetActive(false);
        m_element.txtLevel.text = "";
    }

    void Start()
    {
        Signal.instance.StartStage.connect = SlotStartStage;
        Signal.instance.StartPhase.connect = SlotStartPhase;
    }

    void SlotStartStage(StageManager.LoadData_Stage _data)
    {
        //일반, 어려움, 지옥, 심연, 전설
        Dictionary<string, string> dbString = new();
        dbString.Add("DIFFICULTY_NORMAL", "일반");
        dbString.Add("DIFFICULTY_ELITE", "어려움");
        dbString.Add("DIFFICULTY_GENERAL", "지옥");
        dbString.Add("DIFFICULTY_HERO", "심연");
        dbString.Add("DIFFICULTY_LEGEND", "전설");

        var sf = "[{0}] <size=150%><color=#000000>{1}-{2}";

        GradeType gt = GradeType.NONE + Math.Min(_data.level, 5);
        var key = $"DIFFICULTY_{gt.ToString().ToUpper()}";
        var diff = dbString[key];
        if (_data.level > 5)
            diff += $"{_data.level - 4}";

        m_element.txtLevel.text = string.Format(sf, diff, _data.chapterIdx, _data.stageIdx);
        m_element.btn_challenge.gameObject.SetActive(_data.isBossWait);

        m_element.infoStage.gameObject.SetActive(false);
        m_element.boss.gameObject.SetActive(false);

        m_element.rtIconBoss.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SlotStartPhase(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SlotStartPhase(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SlotStartPhase(3);
    }

    void SlotStartPhase(int _phaseIdx)
    {
        bool isBossPhase = _phaseIdx == 3;
        if (isBossPhase)
        {
            // 도전하기로 보스창이 이미 보인다면 생략한다
            if (m_element.boss.gameObject.activeSelf == false)
                ChageBossPhaseAsync().Forget();
        }
        else if (StageManager.instance.data.isBossWait == false)
        {
            // 보스창이 열려 있으면 초기화 해줘야 할 것들이 있어
            if (m_element.infoStage.gameObject.activeSelf == false)
            {
                m_element.boss.gameObject.SetActive(false);
                m_element.infoStage.gameObject.SetActive(true);

                // 왼쪽에 있으면 오른쪽으로 돌려주자
                var scale = m_element.rtIconBoss.localScale;
                if (scale.x < 0)
                {
                    scale.x *= -1;
                    m_element.rtIconBoss.localScale = scale;
                    m_element.rtIconBoss.rotation = Quaternion.Euler(Vector3.zero);
                    m_element.rtIconBoss.anchoredPosition = m_element.startPosIconBoss;
                }
            }

            m_element.infoStage.SetPhase(_phaseIdx);
        }
    }

    void StopChageBossPhase()
    {
        if (m_ctsChangeBoss != null)
        {
            m_ctsChangeBoss.Cancel();
            m_ctsChangeBoss.Dispose();
            m_ctsChangeBoss = null;
        }

        m_element.infoStage.element.cg.DOKill();
        m_element.rtIconBoss.DOKill();
    }

    CancellationTokenSource m_ctsChangeBoss;
    async UniTask ChageBossPhaseAsync()
    {
        StopChageBossPhase();
        m_ctsChangeBoss = new();

        // 보스아이콘 커졌다 돌진 준비 하기. 회전
        var scale = m_element.rtIconBoss.localScale;

        m_element.rtIconBoss.DORotate(new Vector3(0, 0, 10), 0.2f);
        m_element.rtIconBoss.DOAnchorPosX(m_element.startPosIconBoss.x + 10, 0.2f);
        await m_element.rtIconBoss.DOPunchScale(Vector3.one * 0.2f, 0.2f).AsyncWaitForCompletion().AsUniTask()
            .AttachExternalCancellation(m_ctsChangeBoss.Token);

        // 스테이지 정보 서서히 사라지기
        m_element.infoStage.element.cg.DOFade(0, 0.3f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            // 스테이지 정보 알파값 초기화하기
            m_element.infoStage.gameObject.SetActive(false);
            m_element.infoStage.element.cg.alpha = 1;

            m_element.boss.SetBossInfo();
        });

        // 보스아이콘 오른쪽으로 바라보기
        scale.x *= -1;
        m_element.rtIconBoss.localScale = scale;
        m_element.rtIconBoss.rotation = Quaternion.Euler(new Vector3(0, 0, -10));

        // 왼쪽으로 이동하기
        var rtBoss = m_element.boss.rt;
        await m_element.rtIconBoss.DOAnchorPosX(m_element.targetPosIconBoss, 0.3f).SetEase(Ease.OutCubic)
            .AsyncWaitForCompletion().AsUniTask()
            .AttachExternalCancellation(m_ctsChangeBoss.Token);

        // 보스아이콘 오른쪽으로 바라보기
        //scale.x *= -1;
        //m_element.rtIconBoss.localScale = scale;
        //m_element.rtIconBoss.rotation = Quaternion.Euler(new Vector3(0, 0, -10));

        //m_element.rtIconBoss.DOPunchScale(new Vector3(-0.1f, 0.1f), 0.2f);
    }

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }
#endif
    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtLevel;
        public InfoStage_Boss boss;
        public InfoStage_Stage infoStage;
        public Button btn_challenge;
        public RectTransform rtIconBoss;

        public Vector2 startPosIconBoss;
        public float targetPosIconBoss;

        public void Initialize(Transform _transform)
        {
            txtLevel = _transform.GetComponent<TextMeshProUGUI>("txt_level");
            boss = _transform.GetComponent<InfoStage_Boss>("Boss");
            infoStage = _transform.GetComponent<InfoStage_Stage>("StageInfo");
            btn_challenge = _transform.GetComponent<Button>("btn_challenge");
            rtIconBoss = _transform.GetComponent<RectTransform>("img_iconBoss");

            startPosIconBoss = rtIconBoss.anchoredPosition;
            targetPosIconBoss = ((RectTransform)_transform.Find("targetPos_iconBoss")).anchoredPosition.x;
        }
    }
}
