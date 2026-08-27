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

        m_element.btnTournament.onClick.AddListener(() => PopupManager.instance.OpenPopup(PopupType.LobbyTournament));

        Signal.instance.UnlockStoryMode.connect = SlotUnlockStoryMode;
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

    void SlotUnlockStoryMode()
        => m_element.story.UnlockStoryMode();

    public void SetActive_GuideArrow(bool _isActive, GuideQuestType _guideQuestType = GuideQuestType.NONE)
    {
        if (_isActive == false)
        {
            m_element.guideArrow.gameObject.SetActive(false);
            return;
        }

        m_element.guideArrow.gameObject.SetActive(true);
        switch (_guideQuestType)
        {
            case GuideQuestType.STORYMODE_PLAY:
                m_element.guideArrow.transform.SetParent(m_element.story.transform);
                m_element.guideArrow.transform.localPosition = Vector3.zero;
                break;
            default:
                m_element.guideArrow.gameObject.SetActive(false);
                break;
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    public Banner_Story story => m_element.story;

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public ButtonHelper btnTutorialSkip;
        public ButtonHelper btnTournament;
        public Banner_Story story;
        public Transform guideArrow;

        public void Initialize(Transform _transform)
        {
            btnTutorialSkip = _transform.parent.GetComponent<ButtonHelper>("btn_skip");

            btnTournament = _transform.GetComponent<ButtonHelper>("Right/btn_tournament");
            story = _transform.GetComponent<Banner_Story>("Right/btn_story");

            guideArrow = _transform.Find("GuideArrow");
        }
    }
    #endregion VALIDATA
}
