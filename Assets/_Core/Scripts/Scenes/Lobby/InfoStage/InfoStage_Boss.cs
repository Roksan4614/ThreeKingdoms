using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class InfoStage_Boss : MonoBehaviour, IValidatable
{
    public RectTransform rt => (RectTransform)transform;

    void Awake()
    {
#if UNITY_EDITOR
        if (Configure.instance.isBooted == false)
            return;
#endif

        if (BossRaidWorker.instance.isRunning == true)
            Awake_BossRaid();
        else if (DataManager.dailyDungeon.isRunning == true)
            Awake_DailyDungeon();
    }

    private void Start()
    {
        Signal.instance.UpdageBossHP.connect = SlotUpdateBossHP;
    }

    private void OnDestroy()
    {
        m_ctsTimer = m_ctsTimer.ReleaseCTS();
    }

    public void SetBossInfo()
    {
        gameObject.SetActive(true);
        rt.DOPunchScale(Vector3.one * 0.1f, 0.2f);

        // TODO
        if (BossRaidWorker.instance.isRunning)
            SetBossInfo_BossRaid(true);
        else if (DataManager.dailyDungeon.isRunning)
            SetBossInfo_DailyDungeon(GradeType.Normal);
        else
            m_element.txtName.text = "";

        m_element.rtBar.anchoredPosition = Vector2.zero;
    }

    public void SlotUpdateBossHP((float percent, float hpMax) _data)
    {
        var width = m_element.rtBar.rect.width;
        //m_element.txtPercent.text = _percent == 0 ? "" : $"{_percent * 100:0.#0}%";
        m_element.rtBar.DOKill();
        var target = width * -(1 - _data.percent);
        m_element.rtBar.DOAnchorPosX(target, 0.2f)
            .OnUpdate(() =>
            {
                var p = Mathf.Max(0, 1 + (m_element.rtBar.anchoredPosition.x / width));
                m_element.txtPercent.text = $"{p * 100:0.#0}%";

                //if (m_element.txtHp != null)
                //    m_element.txtHp.text = (_data.hpMax * p).ToString("#,0");
            });

        if (BossRaidWorker.instance.isRunning == true)
            SlotUpdateBossHP_BossRaid(target);

        ShowFxDamage();
    }

    public void OnManualValidate()
    {
        m_element.Initialize(transform);
        m_elementBossRiad.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;

    [Serializable]
    public struct ElementData
    {
        public RectTransform rtBar;
        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtPercent;
        //public TextMeshProUGUI txtHp;

        public void Initialize(Transform _transform)
        {
            rtBar = _transform.GetComponent<RectTransform>("Bar");
            txtPercent = rtBar.GetComponent<TextMeshProUGUI>("txt_percent");
            //txtHp = rtBar.GetComponent<TextMeshProUGUI>("txt_hp");
            txtName = _transform.GetComponent<TextMeshProUGUI>("txt_name");
        }
    }
}
