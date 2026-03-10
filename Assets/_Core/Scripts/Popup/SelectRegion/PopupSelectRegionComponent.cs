using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupSelectRegionComponent : BasePopupComponent
{
    PopupSelectRegionComponent() : base(PopupType.SelectRegion) { }

    PopupSelectRegion_HeroInfo m_popupHeroInfo;

    private void Start()
    {
        PopupManager.instance.ShowDimm(false);

        m_popupHeroInfo = transform.GetComponent<PopupSelectRegion_HeroInfo>("Popup");
        m_popupHeroInfo.gameObject.SetActive(false);

        foreach (var hero in m_element.dbHero)
        {
            hero.btnHero.onClick.AddListener(() => OnButton_RegionAsync(hero.region).Forget());
        }
    }

    async UniTask OnButton_RegionAsync(RegionType _region)
    {
        foreach (var hero in m_element.dbHero)
        {
            if (hero.region != _region)
                StartFade(hero, false);
        }

        m_popupHeroInfo.Open();

        await UniTask.WaitUntil(() => m_popupHeroInfo.gameObject.activeSelf == false);

        foreach (var hero in m_element.dbHero)
        {
            if (hero.region != _region)
                StartFade(hero, true);
        }
    }

    void StartFade(RegionData _region, bool _isIn)
        => StartFadeAsync(_region, _isIn).Forget();

    async UniTask StartFadeAsync(RegionData _region, bool _isIn)
    {
        _region.txtName.gameObject.SetActive(_isIn);

        float alpha = _isIn ? 0 : 1;
        await DOTween.To(() => alpha, _x => alpha = _x, 1 - alpha, 0.2f)
            .OnUpdate(() => _region.SetAlpha(alpha)).AsyncWaitForCompletion();
    }

    public override void OnManualValidate()
        => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        [SerializeField]
        List<RegionData> m_dbHero;
        public List<RegionData> dbHero => m_dbHero;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");

            m_dbHero = new();
            for (var region = RegionType.NONE + 1; region < RegionType.Etc; region++)
            {
                RegionData hero = new();
                hero.Initialize(region, panel.Find("btn_" + region));
                m_dbHero.Add(hero);
            }
        }
    }

    [Serializable]
    struct RegionData
    {
        public RegionType region;

        public RectTransform rt;
        public TextMeshProUGUI txtName;
        public Button btnHero;

        [SerializeField] SpriteRenderer[] renderers;
        [SerializeField] float[] orinAlpha;

        public void Initialize(RegionType _region, Transform _transform)
        {
            region = _region;
            rt = (RectTransform)_transform;
            txtName = _transform.GetComponent<TextMeshProUGUI>("txt_name");
            btnHero = _transform.GetComponent<Button>();

            renderers = rt.GetComponentsInChildren<SpriteRenderer>();
            orinAlpha = new float[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
                orinAlpha[i] = renderers[i].color.a;
        }

        public void SetAlpha(float _alpha)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Color clr = renderers[i].color;
                clr.a = orinAlpha[i] * _alpha;

                renderers[i].color = clr;
            }
        }
    }
}
