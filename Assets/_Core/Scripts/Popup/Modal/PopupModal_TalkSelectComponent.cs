using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PopupModal_TalkSelectComponent : PopupModalComponent
{
    PopupModal_TalkSelectComponent() : base(PopupType.Modal_TalkSelect) { }

    public int selelctOption = -1;

    protected override void Start() { }

    protected override void Update()
    {
        m_elementTalk.mouse.position = CameraManager.instance.GetMousePosition();
    }

    // ModalTalkData
    public override void OpenPopup(params object[] _args)
    {
        isSwitchEscape = false;
        ModalTalkData talkData = (ModalTalkData)_args[0];

        for (int i = 0; i < talkData.options.Length; i++)
        {
            bool isNew = i == m_elementTalk.btnSelect.Count;
            var btnSelect = isNew ?
                Instantiate(m_elementTalk.btnSelect[0], m_elementTalk.rtPanel) :
                m_elementTalk.btnSelect[i];

            if (isNew)
            {
                btnSelect.onClick.RemoveAllListeners();
                m_elementTalk.btnSelect.Add(btnSelect);
            }
            btnSelect.onClick.AddListener(() => OnButton_SelectAsync(btnSelect.transform.GetSiblingIndex()).Forget());
            btnSelect.text = talkData.options[i];
        }

        m_elementTalk.rtPanel.ForceRebuildLayout();
        var size = m_elementTalk.rtPanel.sizeDelta;
        if (size.x > Screen.width * 0.95f)
        {
            m_elementTalk.fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            size.x = Screen.width * 0.95f;

            m_elementTalk.rtPanel.sizeDelta = size;

            m_elementTalk.rtPanel.ForceRebuildLayout();
        }

        for (int i = 0; i < m_elementTalk.btnSelect.Count; i++)
        {
            var btn = m_elementTalk.btnSelect[i];
            btn.SetColliderSize();
            var canvas = btn.GetComponent<CanvasGroup>();
            canvas.alpha = .9f;

            btn.funcEnter = () => canvas.alpha = 1f;
            btn.funcExit = () => canvas.alpha = .9f;
        }

        Utils.SetActivePunch(m_elementTalk.rtPanel, true);
    }

    async UniTask OnButton_SelectAsync(int _selectIndex)
    {
        selelctOption = _selectIndex;

        m_elementTalk.panelBG.DOFade(0f, 0.05f);

        for (int i = 0; i < m_elementTalk.btnSelect.Count; i++)
        {
            var btn = m_elementTalk.btnSelect[i];
            var canvas = btn.GetComponent<CanvasGroup>();
            btn.isTriggerSwitch = btn.interactable = false;

            btn.transform.SetParent(transform);
            if (i != selelctOption)
                Utils.SetActivePunch(btn.transform, false);
        }

        m_elementTalk.dimm.DOFade(0, 0.15f);
        await Utils.SetActivePunchAsync(m_elementTalk.rtPanel, false);
        await UniTask.WaitForSeconds(.5f);
        await Utils.SetActivePunchAsync(m_elementTalk.btnSelect[selelctOption].transform, false);
        //m_elementTalk.btnSelect[selelctOption].gameObject.SetActive(false);
        await UniTask.WaitForSeconds(.3f);
        Close();
    }

    #region VALIDATE
    public override void OnManualValidate() => m_elementTalk.Initialize(transform);

    [SerializeField]
    ElementDataTalk m_elementTalk;

    [Serializable]
    struct ElementDataTalk
    {
        public RectTransform rtPanel;
        public Image dimm;
        public Transform mouse;

        public List<ButtonHelper> btnSelect;

        public ContentSizeFitter fitter;

        public Image panelBG;

        public void Initialize(Transform _transform)
        {
            rtPanel = (RectTransform)_transform.Find("Panel");
            dimm = _transform.GetComponent<Image>("Dimm");
            mouse = _transform.Find("MousePosition");

            btnSelect = rtPanel.GetComponentsInChildren<ButtonHelper>().ToList();

            fitter = rtPanel.GetComponent<ContentSizeFitter>();

            panelBG = rtPanel.GetComponent<Image>();
        }
    }
    #endregion VALIDATA

    public struct ModalTalkData
    {
        public string[] options;
    }
}
