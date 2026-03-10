using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class Scene_Lobby : SceneBase
{
    protected override void Awake()
    {
        base.Awake();
    }

    async void Start()
    {
        await UniTask.WaitForEndOfFrame();
        await DataManager.InitializeAsync();

        // 캐릭터가 없다면 선택 화면부터
        if( DataManager.userInfo.myHero.Count == 0)
            await PopupManager.instance.OpenPopupAndWait(PopupType.SelectRegion);

        await TeamManager.instance.SpawnUpdateAsync();

        StageManager.instance
            .StartStageAsync(() => PopupManager.instance.ShowDimm(false))
            .Forget();
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
