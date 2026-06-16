using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Scene_Lobby : SceneBase
{
    async void Start()
    {
        await UniTask.WaitForEndOfFrame();

        // 캐릭터가 없다면 선택 화면부터
        if (DataManager.userInfo.myHero.Count == 0)
            await PopupManager.instance.OpenPopupAndWait(PopupType.SelectRegion);

        await TeamManager.instance.SpawnUpdateAsync();

        if (TutorialManager.instance.IsComplete(TutorialType.START) == false)
            await TutorialManager.instance.StartAsync(TutorialType.START);

#if UNITY_EDITOR
        StageManager.instance.TestDevSelectAsync().Forget();
#endif

        StageManager.instance.StartStageAsync().Forget();

        ControllerManager.instance.SetSwitch(true);
    }

    public override void OnManualValidate() { m_element.Initialize(transform); }

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public void Initialize(Transform _transform)
        {
        }
    }
}
