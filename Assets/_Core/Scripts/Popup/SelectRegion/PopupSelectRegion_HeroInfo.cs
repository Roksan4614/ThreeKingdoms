using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupSelectRegion_HeroInfo : MonoBehaviour, IValidatable
{
    Transform m_trnsHero;
    Vector3 m_prevPos;

    private void Awake()
    {
        transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(() => CloseAsync().Forget());
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
        _regionData.SetActiveName(false);

        m_prevPos = _regionData.rt.position;
        m_trnsHero = _regionData.rt;
        m_trnsHero.DOMove(m_element.posCharacter.position, 0.2f);
        m_trnsHero.DOScale(Vector3.one, 0.2f);

        gameObject.SetActive(true);
        Utils.SetActivePunch(transform, true);

        var isFlipPrev = _regionData.heroComponent.move.isFlip;
        _regionData.heroComponent.move.SetFlip(true);

        SetRegionData(_regionData);

        await UniTask.WaitForSeconds(0.2f);

        await UniTask.WaitUntil(() => gameObject.activeSelf == false);
        _regionData.heroComponent.move.SetFlip(isFlipPrev);
        _regionData.SetActiveName(true);
    }

    async UniTask CloseAsync()
    {
        await Utils.SetActivePunchAsync(transform, false);
        gameObject.SetActive(false);
        transform.localScale = Vector3.one;

        m_trnsHero.DOMove(m_prevPos, 0.2f);
        m_trnsHero.DOScale(Vector3.one * 0.8f, 0.2f);
    }

    void SetRegionData(PopupSelectRegionComponent.RegionData _regionData)
    {
        var dbHeroData = TableManager.hero.Get(_regionData.keyMaster);

        //// FRONT PANEL
        var key = $"{dbHeroData.regionType}_{_regionData.keyMaster}".ToUpper();
        m_element.txtName.text = $"{TableManager.stringHero.GetString("NAME_" + key)}<size=80%><color=#888888> {TableManager.stringHero.GetString("COURTESY_" + key)}";
        m_element.txtTalk.text = _regionData.masterTalk;
        m_element.txtDesc.text = _regionData.masterDesc;
        m_element.txtDescSub.text = _regionData.masterDescSub;
    }

    public void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform posCharacter;

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtTalk;
        public TextMeshProUGUI txtDesc;
        public TextMeshProUGUI txtDescSub;


        public void Initialize(Transform _trnsform)
        {
            posCharacter = _trnsform.Find("Panel/PosCharacter");

            var front = _trnsform.Find("Panel/FrontPanel");
            txtName = front.GetComponent<TextMeshProUGUI>("txt_name");
            txtTalk = front.GetComponent<TextMeshProUGUI>("txt_talk");
            txtDesc = front.GetComponent<TextMeshProUGUI>("txt_desc");
            txtDescSub = front.GetComponent<TextMeshProUGUI>("txt_descSub");
        }
    }
}
