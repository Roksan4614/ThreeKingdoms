using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Scene_StoryMode : SceneBase
{
    private void Start()
    {
        InfoStageComponent.instance.gameObject.SetActive(false);

        StartAsync().Forget();
    }

    async UniTask StartAsync()
    {
        await UniTask.NextFrame();

        List<UniTask> tasks = new();
        tasks.Add(StageManager.instance.InitializeAsync_StoryMode());

        await UniTask.WhenAll(tasks.ToArray());

        isReady = true;

        DataManager.storyMode.SetPlayingMode(DataManager.storyMode.isPlayingMode == false, false);
        OnButton_Playing();

        m_element.btnPlay.onClick.AddListener(OnButton_Playing);
        m_element.btnSkip.onClick.AddListener(() => OnButtonAsync_Skip().Forget());

        //setlocalization
        {
            m_element.btnPlay.text = TableManager.stringTable.GetString("UI_AUTO_PLAY");
            m_element.btnSkip.text = TableManager.stringTable.GetString("UI_SKIP");
        }
    }

    public async UniTask OnButtonAsync_Skip(UnityAction<StatusType> _callback = null)
    {
        if (StageManager.instance.slotStory.resultTalkIdx == -1 && TableManager.storyChoice.GetChoices(DataManager.storyMode.curNodeKey).Length > 0)
        {
            //건너띄기할_수_없습니다.
            PopupManager.instance.AlertShow_Table("CAN_NOT_SKIP");
            return;
        }

        m_element.btnSkip.interactable = false;

        // "건너띄겠습니까?"
        var result = await PopupManager.instance.OpenModalAsync_Table("MODAL_SKIP");

        _callback?.Invoke(result);

        if (result == StatusType.Success)
            DataManager.storyMode.ExitAsync(StageManager.instance.slotStory.resultTalkIdx).Forget();
        else
            m_element.btnSkip.interactable = true;
    }

    void OnButton_Playing()
    {
        bool isPlaying = DataManager.storyMode.isPlayingMode == false;
        DataManager.storyMode.SetPlayingMode(isPlaying);

        m_element.btnPlay.SetDrawSelect(isPlaying);
        m_element.btnPlay.text = TableManager.stringTable.GetString(isPlaying ? "BUTTON_PLAY_RUN" : "BUTTON_PLAY");

        m_element.btnPlay.transform.ForceRebuildLayout(1);
    }

    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_element.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    protected ElementData m_element;

    [Serializable]
    protected struct ElementData
    {
        public ButtonHelper btnSkip;
        public ButtonHelper btnPlay;

        public void Initialize(Transform _transform)
        {
            var menu = _transform.Find("Canvas/SafeArea/Menu");

            btnSkip = menu.GetComponent<ButtonHelper>("btn_skip");
            btnPlay = menu.GetComponent<ButtonHelper>("btn_play");
        }
    }
}
