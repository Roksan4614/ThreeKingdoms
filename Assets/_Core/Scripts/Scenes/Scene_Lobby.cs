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

        await TeamManager.instance.SpawnUpdateAsync();

        IngameLog.Add("tt: 2");
        await UniTask.WaitForEndOfFrame();

        IngameLog.Add("tt: 2.1");
        PopupManager.instance.ShowDimm(false);

        IngameLog.Add("tt: 3");
        StageManager.instance.StartStageAsync().Forget();

        IngameLog.Add("tt: 4");
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
