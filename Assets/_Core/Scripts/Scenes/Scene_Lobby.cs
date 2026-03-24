using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Scene_Lobby : SceneBase
{
    async void Start()
    {
        Signal.instance.ActiveHUD.connectLambda = new(this, _isActive =>
        {
            m_element.heroInfo.gameObject.SetActive(_isActive);
        });

        await UniTask.WaitForEndOfFrame();

        // 캐릭터가 없다면 선택 화면부터
        if (DataManager.userInfo.myHero.Count == 0)
            await PopupManager.instance.OpenPopupAndWait(PopupType.SelectRegion);

        await TeamManager.instance.SpawnUpdateAsync();

        if (TutorialManager.instance.IsComplete(TutorialType.START) == false)
            await TutorialManager.instance.StartAsync(TutorialType.START);

        StageManager.instance.StartStageAsync().Forget();

        ControllerManager.instance.isSwitch = true;

        m_element.btnAuto.onClick.AddListener(OnButton_Auto);
        SetAutoUIAsync().Forget();
    }

    void OnButton_Auto()
    {
        DataManager.option.isAutoSkill = !DataManager.option.isAutoSkill;
        SetAutoUIAsync().Forget();
    }

    CancellationTokenSource m_ctsAutoSkill;
    async UniTask SetAutoUIAsync()
    {
        if (m_ctsAutoSkill != null)
        {
            m_ctsAutoSkill.Cancel();
            m_ctsAutoSkill.Dispose();
            m_ctsAutoSkill = null;
        }

        bool isAutoSkill = DataManager.option.isAutoSkill;

        var rtAuto = m_element.imgAuto.rectTransform;
        var txt = m_element.btnAuto.GetTMPText();

        ColorUtility.TryParseHtmlString(isAutoSkill ? "#000000" : "#a4a4a4", out Color color);
        m_element.imgAuto.color = color;
        txt.color = color;
        m_element.outline.effectColor = color;

        if (DataManager.option.isAutoSkill)
        {
            m_ctsAutoSkill = new();
            var token = m_ctsAutoSkill.Token;

            while (true)
            {
                rtAuto.Rotate(0, 0, -200 * Time.deltaTime);
                await UniTask.WaitForEndOfFrame(cancellationToken: token);
            }
        }
        else
            rtAuto.rotation = Quaternion.Euler(0, 0, 0);
    }


    public override void OnManualValidate() { m_element.Initialize(transform); }

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform heroInfo;

        public ButtonHelper btnAuto;
        public Image imgAuto;
        public Outline outline;

        public void Initialize(Transform _trnsform)
        {
            heroInfo = _trnsform.Find("Canvas/HeroInfo");

            btnAuto = heroInfo.GetComponent<ButtonHelper>("btn_auto");
            imgAuto = btnAuto.transform.GetComponent<Image>("img_auto");
            outline = imgAuto.transform.GetComponent<Outline>();
        }
    }
}
