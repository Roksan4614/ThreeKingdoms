using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InfoStage_Boss : MonoBehaviour, IValidatable
{
    public RectTransform rt => (RectTransform)transform;

    private void Start()
    {
        Signal.instance.UpdageBossHP.connect = SlotUpdateBossHP;
    }

    public void SetBossInfo()
    {
        gameObject.SetActive(true);
        rt.DOPunchScale(Vector3.one * 0.1f, 0.2f);

        m_element.txtName.text = "";

        m_element.rtBar.anchoredPosition = Vector2.zero;
    }

    void SlotUpdateBossHP(float _percent)
    {
        m_element.txtPercent.text = _percent == 0 ? "" : $"{_percent * 100:0.#0}%";
        m_element.rtBar.DOKill();
        m_element.rtBar.DOAnchorPosX(m_element.rtBar.rect.width * - (1 - _percent), 0.2f);
    }

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }
#endif

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
