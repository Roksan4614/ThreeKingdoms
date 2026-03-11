using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using static PopupSelectRegionComponent;
using static UnityEditor.PlayerSettings;

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
        _regionData.txtName.gameObject.SetActive(false);

        m_prevPos = _regionData.rt.position;
        m_trnsHero = _regionData.rt;
        m_trnsHero.DOMove(m_element.posCharacter.position, 0.2f);
        m_trnsHero.DOScale(Vector3.one, 0.2f);

        gameObject.SetActive(true);
        Utils.SetActivePunch(transform, true);

        var isFlipPrev = _regionData.heroComponent.move.isFlip;
        _regionData.heroComponent.move.SetFlip(true);

        await UniTask.WaitForSeconds(0.2f);

        await UniTask.WaitUntil(() => gameObject.activeSelf == false);
        _regionData.heroComponent.move.SetFlip(isFlipPrev);
        _regionData.txtName.gameObject.SetActive(true);
    }

    async UniTask CloseAsync()
    {
        await Utils.SetActivePunchAsync(transform, false);
        gameObject.SetActive(false);
        transform.localScale = Vector3.one;

        m_trnsHero.DOMove(m_prevPos, 0.2f);
        m_trnsHero.DOScale(Vector3.one * 0.8f, 0.2f);
    }

    public void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform posCharacter;

        public void Initialize(Transform _trnsform)
        {
            posCharacter = _trnsform.Find("Panel/PosCharacter");
        }
    }
}
