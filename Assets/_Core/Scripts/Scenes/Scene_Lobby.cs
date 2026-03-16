using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class Scene_Lobby : SceneBase
{
    async void Start()
    {
        Signal.instance.ActiveHUD.connectLambda = new(this, _isActive => {
            m_elementBase.canvas.transform.Find("HeroInfo").gameObject.SetActive(false);
        });

        await UniTask.WaitForEndOfFrame();

        // 캐릭터가 없다면 선택 화면부터
        if( DataManager.userInfo.myHero.Count == 0)
            await PopupManager.instance.OpenPopupAndWait(PopupType.SelectRegion);

        await TeamManager.instance.SpawnUpdateAsync();

        if (TutorialManager.instance.IsComplete(TutorialType.START) == false)
            await TutorialManager.instance.StartAsync(TutorialType.START);

        StageManager.instance
            .StartStageAsync(() => PopupManager.instance.ShowDimm(false))
            .Forget();

        ControllerManager.instance.isSwitch = true;
    }

    //public override void OnManualValidate()
    //{
    //    base.OnManualValidate();

    //    m_element.Initialize(transform);
    //}

    //[SerializeField, HideInInspector]
    //ElementData m_element;

    //[Serializable]
    //struct ElementData
    //{
    //    [SerializeField]
    //    Canvas m_canvas;
    //    public Canvas canvas => m_canvas;

    //    public void Initialize(Transform _trnsform)
    //    {
    //        m_canvas = _trnsform.GetComponent<Canvas>("Canvas");
    //    }
    //}
}
