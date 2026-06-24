using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scene_BossRaid : SceneBase
{
    private void Start()
    {
        StartAsync().Forget();

        m_element.dimmResult.gameObject.SetActive(false);

        Signal.instance.ActiveHUD.connectLambda = new(this, _isActive => { });
    }

    async UniTask StartAsync()
    {
        await UniTask.NextFrame();

        List<UniTask> tasks = new();
        tasks.Add(TeamManager.instance.SpawnUpdateAsync());
        tasks.Add(StageManager.instance.InitializeAsync_BossRaid());

        await UniTask.WhenAll(tasks);

        BossRaidWorker.instance.StartBossRaid();

        PopupManager.instance.ShowDimm(false);

    }

    public void SetActiveResult(bool _isActive, bool _isWithTween)
    {
        m_element.panelResult.localScale = Vector3.one;

        if (_isActive)
            m_element.txtResult.text = BossRaidWorker.instance.isSuccessed ? "처치성공" : "처치실패";

        if (_isWithTween)
        {
            var canvasGroup = m_element.panelResult.GetComponent<CanvasGroup>();
            if (_isActive == true)
            {
                canvasGroup.alpha = 1;
                m_element.dimmResult.gameObject.SetActive(true);
                m_element.dimmResult.DOFillAmount(1, 0.1f);
                Utils.SetActivePunch(m_element.panelResult, true);
            }
            else
            {
                m_element.dimmResult.DOFillAmount(0, 0.1f).OnUpdate(() =>
                {
                    canvasGroup.alpha = m_element.dimmResult.color.a;
                }).OnComplete(() => m_element.dimmResult.gameObject.SetActive(false));
                m_element.panelResult.DOScale(2f, 0.1f);
            }
        }
        else
            m_element.dimmResult.gameObject.SetActive(_isActive);
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
