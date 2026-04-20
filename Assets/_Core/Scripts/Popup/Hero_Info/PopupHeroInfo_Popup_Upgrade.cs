using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo_Popup_Upgrade : MonoBehaviour, IValidatable
{
    enum UpgradeType
    {
        Upgrade,
        Enchant
    }

    float m_startPosY;
    StatusType m_status = StatusType.Wait;
    public bool isSuccessed => m_status == StatusType.Success;

    private void Awake()
    {
        m_startPosY = m_element.panel.anchoredPosition.y;

        var size = m_element.panel.sizeDelta;
        size.y = Screen.height * 0.5f + m_startPosY;
        m_element.panel.sizeDelta = size;

        m_element.dimm.onClick.AddListener(Close);
    }

    void SetInfo(UpgradeType _type)
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_isUpgrade">Upgrade: true / Enchant: false</param>
    /// <returns></returns>
    public async UniTask OpenAsyn(bool _isUpgrade)
    {
        m_element.dimm.interactable = true;
        m_status = StatusType.Wait;
        gameObject.SetActive(true);

        SetInfo(_isUpgrade ? UpgradeType.Upgrade : UpgradeType.Enchant);

        var pos = m_element.panel.anchoredPosition;
        pos.y = m_startPosY - m_element.panel.sizeDelta.y;

        m_element.panel.anchoredPosition = pos;

        await m_element.panel.DOAnchorPosY(m_startPosY, .1f).SetEase(Ease.OutCubic).AsyncWaitForCompletion();

        await UniTask.WaitUntil(() => gameObject.activeSelf == false, cancellationToken: destroyCancellationToken);
    }

    public void Close()
    {
        m_element.dimm.interactable = false;
        var targetPosY = m_startPosY - m_element.panel.sizeDelta.y;

        m_element.panel.DOAnchorPosY(targetPosY, .1f).SetEase(Ease.InBack)
            .OnComplete(() => gameObject.SetActive(false));
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public RectTransform panel;
        public Button dimm;

        public void Initialize(Transform _transform)
        {
            panel = (RectTransform)_transform.Find("Panel");
            dimm = _transform.GetComponent<Button>("Dimm");
        }
    }
    #endregion VALIDATA
}
