using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EffectWorker : Singleton<EffectWorker>
{
    [Serializable]
    struct ElementData
    {
        public Transform renderer;
        public Transform canvas;

        public Text baseDamage;
        public SpriteAnimaion baseDamageHit;
    }

    [SerializeField]
    ElementData m_element;

    enum EffectType
    {
        damage,
        //bounty,
        hit,
        //die,
    }

    [SerializeField]
    Color m_colorCritical;
    Color m_colorHit;

    Dictionary<EffectType, List<Text>> m_fx_text = new()
    {
        { EffectType.damage, new List<Text>() },
        //{ EffectType.bounty, new List<TextMeshProUGUI>() },
    };

    Dictionary<EffectType, List<SpriteAnimaion>> m_fx_animation = new()
    {
        { EffectType.hit, new List<SpriteAnimaion>() },
        //{ EffectType.die, new List<SpriteAnimaion>() },
    };

#if UNITY_EDITOR
    private void OnValidate()
    {
        m_element.canvas = transform.Find("Canvas");
        m_element.renderer = transform.Find("Renderer");

        m_element.baseDamage = transform.GetComponent<Text>("Canvas/Damage");
        m_element.baseDamageHit = transform.GetComponent<SpriteAnimaion>("Renderer/DamageHit");

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void Start()
    {
        m_element.baseDamage.gameObject.SetActive(false);
        m_element.baseDamageHit.gameObject.SetActive(false);

        m_fx_text[EffectType.damage].Add(m_element.baseDamage);
        m_fx_animation[EffectType.hit].Add(m_element.baseDamageHit);

        m_colorHit = m_element.baseDamageHit.GetColor();
    }

    public void SlotDamageTakenEffect(HitData _hitData)
    {
        //if (m_fx_text[EffectType.damage] == null)
        //    return;

        // 데미지 표시해주자
        if (_hitData.value != 0)
        {
            var targetParent = _hitData.target.element.effect_canvas;

            Text txtdamage = m_fx_text[EffectType.damage].Find(x => x.gameObject.activeSelf == false);

            if (txtdamage == null)
            {
                txtdamage = Instantiate(m_fx_text[EffectType.damage][0], m_element.canvas)
                    .GetComponent<Text>();
                txtdamage.name = $"Damage_{m_fx_text[EffectType.damage].Count}";
                txtdamage.transform.localScale = new Vector3(1f, 0.9f, 1f);
                m_fx_text[EffectType.damage].Add(txtdamage);
            }

            var trns = txtdamage.transform;

            txtdamage.text = $"<size={txtdamage.fontSize * (_hitData.isCritical ? 1.5 : 1)}><color=#{(_hitData.value > 0 ? "A5FFAB>+" : _hitData.isCritical ? $"E58B00>" : _hitData.isAlliance ? "0B7FC6>" : "9F2625>")}{_hitData.value}</color></size>";
            trns.SetParent(targetParent);
            trns.SetAsLastSibling();

            if (_hitData.isCritical)
                trns.DOPunchScale(Vector3.one * 1.2f, 0.2f);

            StartCoroutine(DoShowEffectValue(txtdamage.gameObject, _hitData.target.transform.position,
                () => trns.SetParent(m_element.canvas)));
        }

        // effect 연출해주자
        //if (_hitData.value < 0)
        {
            var targetParent = _hitData.target.element.effect_renderer;

            SpriteAnimaion animation = m_fx_animation[EffectType.hit].Find(x => x.gameObject.activeSelf == false);
            if (animation == null)
            {
                animation = Instantiate(m_fx_animation[EffectType.hit][0], m_element.renderer)
                    .GetComponent<SpriteAnimaion>();
                animation.name = $"DamageHit_{m_fx_animation[EffectType.hit].Count}";
                m_fx_animation[EffectType.hit].Add(animation);
            }

            var angle = Vector3.Angle(Vector3.right, _hitData.target.transform.position - _hitData.attacker.position);
            if (_hitData.target.transform.position.y < _hitData.attacker.position.y)
                angle = 360 - angle;

            animation.SetColor(_hitData.isCritical ? m_colorCritical : m_colorHit);

            var trns = animation.transform;

            trns.localEulerAngles = new Vector3(0, 0, angle - 90);
            trns.localScale = _hitData.isCritical ? new Vector3(1.2f, 1.2f) : Vector3.one;

            trns.SetParent(targetParent);
            trns.SetAsLastSibling();
            trns.position = targetParent.position + new Vector3(0, .5f);
            animation.Play(() => trns.SetParent(m_element.renderer));
        }
    }

    [SerializeField]
    float fPosY = 20f;
    IEnumerator DoShowEffectValue(GameObject _damageObject, Vector3 _posTarget, Action _onCompleted)
    {
        _damageObject.SetActive(true);
        Utils.SetObjectAlpha(_damageObject, 1f);
        _damageObject.transform.position =
            _posTarget + new Vector3(UnityEngine.Random.Range(-.5f, .5f), fPosY);

        var rt = _damageObject.GetComponent<RectTransform>();


        var cg = _damageObject.GetComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = 1;

        yield return rt.DOAnchorPos(
            rt.anchoredPosition + new Vector2(0, UnityEngine.Random.Range(20, 50f))
            , 0.5f).SetEase(Ease.OutCubic).WaitForCompletion();

        if (cg != null)
            yield return cg.DOFade(0f, 0.2f).SetEase(Ease.OutCubic).WaitForCompletion();

        _damageObject.SetActive(false);
        _onCompleted?.Invoke();
    }

    //void SlotBountyEffect(EffectData _effectData)
    //{
    //    TextMeshProUGUI txtBounty = m_fx_text[EffectType.bounty].Find(x => x.gameObject.activeSelf == false);

    //    if (txtBounty == null)
    //    {
    //        txtBounty = Instantiate(m_baseBounty, transform).GetComponent<TextMeshProUGUI>();
    //        m_fx_text[EffectType.bounty].Add(txtBounty);
    //    }
    //    else
    //        txtBounty.transform.SetAsLastSibling();

    //    txtBounty.text = $"{(_effectData.value > 0 ? "+" : "")}{_effectData.value}";
    //    txtBounty.transform.SetParent(_effectData.target.Find("Canvas/Effect"));

    //    StartCoroutine(DoShowEffectValue(txtBounty.gameObject, _effectData.target.position));
    //}

    //IEnumerator DoInstantiateEffect(EffectData _effectData)
    //{
    //    var effect = _effectData.attacker;
    //    effect.SetParent(transform.Find("World"));
    //    effect.rotation = Quaternion.identity;

    //    yield return new WaitUntil(() => effect.gameObject.activeSelf == false);

    //    effect.SetParent(_effectData.target);
    //}

    public void SlotDeleteBrokenEffect()
    {
        foreach (var fx in m_fx_text)
        {
            for (int i = 0; i < fx.Value.Count; i++)
            {
                if (fx.Value[i].Equals(null))
                {
                    fx.Value.RemoveAt(i);
                    i--;
                }
            }
        }

        foreach (var fx in m_fx_animation)
        {
            for (int i = 0; i < fx.Value.Count; i++)
            {
                if (fx.Value[i].Equals(null))
                {
                    fx.Value.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public struct HitData
    {
        public Transform attacker;
        public CharacterComponent target;
        public int value;
        public bool isCritical;
        public bool isAlliance;
    }
}
