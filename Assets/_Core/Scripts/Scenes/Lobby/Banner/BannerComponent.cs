using System;
using UnityEngine;
using UnityEngine.Events;

public class BannerComponent : Singleton<BannerComponent>, IValidatable
{
    protected override void OnAwake()
    {
        Signal.instance.ActiveHUD.connectLambda = new(this, _isActive =>
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(_isActive);
        });
    }

    public void AddListenerSkip(UnityAction _onClick)
    {
        SetActiveSkip(true);
        m_element.btnTutorialSkip.onClick.RemoveAllListeners();
        m_element.btnTutorialSkip.onClick.AddListener(() =>
        {
            SetActiveSkip(false);
            _onClick();
        });
    }

    public void SetActiveSkip(bool _isActive)
        => m_element.btnTutorialSkip.gameObject.SetActive(_isActive);

    public void RedDot_StoryMode()
    {
        m_element.story.Reddot();
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public ButtonHelper btnTutorialSkip;

        public Banner_Story story;

        public void Initialize(Transform _transform)
        {
            btnTutorialSkip = _transform.parent.GetComponent<ButtonHelper>("btn_skip");

            story = _transform.GetComponent<Banner_Story>("Right/btn_story");
        }
    }
    #endregion VALIDATA
}
