using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scene_DailyDungeon : SceneBase
{
    private void Start()
    {
        StartAsync().Forget();

        m_element.dimmResult.gameObject.SetActive(false);

        //Signal.instance.ActiveHUD.connectLambda = new(this, _isActive => { });
    }

    async UniTask StartAsync()
    {
        await UniTask.NextFrame();

        List<UniTask> tasks = new();
        tasks.Add(TeamManager.instance.SpawnUpdateAsync());
        tasks.Add(StageManager.instance.InitializeAsync_DailyDungeon());

        await UniTask.WhenAll(tasks);

        DataManager.dailyDungeon.Start();

        PopupManager.instance.ShowDimm(false);

        await UniTask.WaitForSeconds(1f);

        // 좀 기다렸다가 전투하게 하자
        StageManager.instance.SetState(CharacterStateType.Battle);
        TeamManager.instance.StartPhase(false);
    }

    #region VALIDATE
    public override void OnManualValidate() { m_elementBase.Initialize(transform); m_element.Initialize(transform); }

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Image dimmResult;
        public TextMeshProUGUI txtResult;

        public Transform panelResult => txtResult.transform.parent;

        public void Initialize(Transform _transform)
        {
            txtResult = _transform.GetComponent<TextMeshProUGUI>("Canvas/Result/Panel/Text");
            dimmResult = txtResult.transform.parent.parent.GetComponent<Image>();
        }
    }
    #endregion VALIDATE
}
