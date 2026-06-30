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

        Awake_BossRaid();
    }

    private void Start()
    {
        Signal.instance.UpdageBossHP.connect = SlotUpdateBossHP;
    }

    private void OnDestroy()
    {
        m_ctsBossRaid = m_ctsBossRaid.ReleaseCTS();
    }

    public void SetBossInfo()
    {
        gameObject.SetActive(true);
        rt.DOPunchScale(Vector3.one * 0.1f, 0.2f);

        // TODO
        if (BossRaidWorker.instance.isRunning)
            StartBossRaid(true);
        else
            m_element.txtName.text = "";

        m_element.rtBar.anchoredPosition = Vector2.zero;
    }

    void SlotUpdateBossHP(float _percent)
    {
        var width = m_element.rtBar.rect.width;
        //m_element.txtPercent.text = _percent == 0 ? "" : $"{_percent * 100:0.#0}%";
        m_element.rtBar.DOKill();
        var target = width * -(1 - _percent);
        m_element.rtBar.DOAnchorPosX(target, 0.2f)
            .OnUpdate(() =>
            {
                var p = 1 + (m_element.rtBar.anchoredPosition.x / width);
                m_element.txtPercent.text = $"{p * 100:0.#0}%";
            });

        if (BossRaidWorker.instance.isRunning == true)
            SlotUpdateBossHP_BossRaid(target);
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

        public void Initialize(Transform _transform)
        {
            rtBar = _transform.GetComponent<RectTransform>("Bar");
            txtPercent = rtBar.GetComponent<TextMeshProUGUI>("txt_percent");
            txtName = _transform.GetComponent<TextMeshProUGUI>("txt_name");
        }
    }
}
