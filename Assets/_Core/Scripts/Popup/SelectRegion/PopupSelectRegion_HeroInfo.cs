using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
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

    public async UniTask OpenAsync(PopupSelectRegionComponent.RegionData _regionData)
    {
        await UniTask.WaitForSeconds(0.2f);

        m_trnsHero = _regionData.rt;
        m_prevPos = _regionData.rt.position;
        _regionData.rt.DOMove(m_element.posCharacter.position, 0.2f);

        gameObject.SetActive(true);
        Utils.SetActivePunch(transform, true);

        await UniTask.WaitUntil(() => gameObject.activeSelf == false);
    }

    async UniTask CloseAsync()
    {
        m_trnsHero.DOMove(m_prevPos, 0.2f);
        await Utils.SetActivePunchAsync(transform, true);
        gameObject.SetActive(false);
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
