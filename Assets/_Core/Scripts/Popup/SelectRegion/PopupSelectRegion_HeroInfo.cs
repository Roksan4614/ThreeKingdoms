using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupSelectRegion_HeroInfo : MonoBehaviour, IValidatable
{
    PopupSelectRegionComponent.RegionData m_regionData;
    Vector3 m_prevPos;

    private void Awake()
    {
        m_element.btnClose.onClick.AddListener(() => CloseAsync().Forget());
        m_element.btnConfirm.onClick.AddListener(() => OnButton_ConfirmAsync().Forget());
    }

    CancellationTokenSource m_cts;

    void OnEnable()
    {
        m_cts = new();
        Utils.WaitEscape(this, () => CloseAsync().Forget(), _token: m_cts.Token);
    }

    void OnDisable()
    {
        m_cts.Cancel();
        m_cts.Dispose();
        m_cts = null;
    }

    public async UniTask OpenAsync(PopupSelectRegionComponent.RegionData _regionData)
    {
        m_regionData = _regionData;
        m_regionData.SetActiveName(false);

        m_prevPos = m_regionData.rt.position;
        m_regionData.rt.DOMove(m_element.posCharacter.position, 0.2f);
        m_regionData.rt.DOScale(Vector3.one, 0.2f);

        Utils.SetActivePunch(transform, true);

        var isFlipPrev = _regionData.heroComponent.move.isFlip;
        _regionData.heroComponent.move.SetFlip(true);

        SetRegionData();

        await UniTask.WaitForSeconds(0.2f);

        await UniTask.WaitUntil(() => gameObject.activeSelf == false);
        _regionData.heroComponent.move.SetFlip(isFlipPrev);
        _regionData.SetActiveName(true);

        await UniTask.WaitForSeconds(0.1f);
    }

    async UniTask CloseAsync()
    {
        await Utils.SetActivePunchAsync(transform, false);
        transform.localScale = Vector3.one;

        m_regionData.rt.DOMove(m_prevPos, 0.2f);
        m_regionData.rt.DOScale(Vector3.one * 0.8f, 0.2f);
    }

    void SetRegionData()
    {
        var dbHeroData = TableManager.hero.Get(m_regionData.keyMaster);

        //// FRONT PANEL
        m_element.txtName.text = $"{dbHeroData.name}<size=80%><color=#888888> {TableManager.stringHero.GetString("COURTESY_" + dbHeroData.regionKey)}";
        m_element.txtTalk.text = m_regionData.masterTalk;
        m_element.txtDesc.text = m_regionData.masterDesc;
        m_element.txtDescSub.text = m_regionData.masterDescSub;

        var startHeroKey = TableManager.region.Get(m_regionData.region).startHeroKey;
        for (int i = 0; i < m_element.startHero.Length; i++)
        {
            HeroInfoData data = new(startHeroKey[i]);

            m_element.startHero[i].SetHeroData(data, null, null);
            m_element.startHero[i].element.SetActiveName(true);
        }
    }

    async UniTask OnButton_ConfirmAsync()
    {
        var result = await PopupManager.instance.OpenModalAsync();

        if (result == StatusType.Success)
        {
            m_regionData.heroComponent.anim.Play(CharacterAnimType.Attack);
            await UniTask.WaitForSeconds(0.5f);

            await DataManager.userInfo.AddHeroAsync(m_regionData.keyMaster, GradeType.Normal, true, true);
            await PopupManager.instance.ShowDimmAsync(true, _durationWait: 0);
            gameObject.SetActive(false);

            await UniTask.WaitForSeconds(1f);
        }
    }

    public void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform posCharacter;

        public Button btnClose;
        public Button btnConfirm;

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtTalk;
        public TextMeshProUGUI txtDesc;
        public TextMeshProUGUI txtDescSub;

        public HeroIconComponent[] startHero;

        public void Initialize(Transform _transform)
        {
            posCharacter = _transform.Find("Panel/PosCharacter");

            btnClose = _transform.GetComponent<Button>("Panel/btn_close");
            btnConfirm = _transform.GetComponent<Button>("Panel/Buttons/btn_start");

            var front = _transform.Find("Panel/FrontPanel");
            txtName = front.GetComponent<TextMeshProUGUI>("txt_name");
            txtTalk = front.GetComponent<TextMeshProUGUI>("txt_talk");
            txtDesc = front.GetComponent<TextMeshProUGUI>("txt_desc");
            txtDescSub = front.GetComponent<TextMeshProUGUI>("txt_descSub");

            startHero = _transform.Find("Panel/StartHero").GetComponentsInChildren<HeroIconComponent>();
        }
    }
}
