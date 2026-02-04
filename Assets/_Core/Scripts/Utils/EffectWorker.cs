using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EffectWorker : MonoSingleton<EffectWorker>
{
    enum EffectType
    {
        damage,
        //bounty,
        hit,
        //die,
    }

    Transform m_parentRenderer;
    Transform m_parentCanvas;

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

    private void Start()
    {
        m_parentCanvas = transform.Find("Canvas");
        {
            var baseDamage = m_parentCanvas.GetComponent<Text>("Damage");
            m_fx_text[EffectType.damage].Add(baseDamage);
            baseDamage.gameObject.SetActive(false);
        }

        m_parentRenderer = transform.Find("Renderer");
        {
            var baseHit = m_parentRenderer.GetComponent<SpriteAnimaion>("DamageHit");
            m_fx_animation[EffectType.hit].Add(baseHit);

            m_colorHit = baseHit.GetColor();
            baseHit.gameObject.SetActive(false);
        }

        //m_baseEffectDie = transform.GetComponent<SpriteAnimaion>("Panel/effect_die");
        //m_baseEffectDie.gameObject.SetActive(false);
    }

    public void SlotDamageTakenEffect(HitData _hitData)
    {
        //if (m_fx_text[EffectType.damage] == null)
        //    return;

        // 데미지 표시해주자
        if (_hitData.value != 0)
        {
            var targetParent = _hitData.target.Find("Character/Canvas/Effect");

            Text txtdamage = m_fx_text[EffectType.damage].Find(x => x.gameObject.activeSelf == false);

            if (txtdamage == null)
            {
                txtdamage = Instantiate(m_fx_text[EffectType.damage][0], m_parentCanvas)
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

            StartCoroutine(DoShowEffectValue(txtdamage.gameObject, _hitData.target.position,
                () => trns.SetParent(m_parentCanvas)));
        }

        // effect 연출해주자
        //if (_hitData.value < 0)
        {
            var targetParent = _hitData.target.Find("Character/Effect_Renderer");

            SpriteAnimaion animation = m_fx_animation[EffectType.hit].Find(x => x.gameObject.activeSelf == false);
            if (animation == null)
            {
                animation = Instantiate(m_fx_animation[EffectType.hit][0], m_parentRenderer)
                    .GetComponent<SpriteAnimaion>();
                animation.name = $"DamageHit_{m_fx_animation[EffectType.hit].Count}";
                m_fx_animation[EffectType.hit].Add(animation);
            }

            var angle = Vector3.Angle(Vector3.right, _hitData.target.position - _hitData.attacker.position);
            if (_hitData.target.position.y < _hitData.attacker.position.y)
                angle = 360 - angle;

            animation.SetColor(_hitData.isCritical ? m_colorCritical : m_colorHit);

            var trns = animation.transform;

            trns.localEulerAngles = new Vector3(0, 0, angle - 90);
            trns.localScale = _hitData.isCritical ? new Vector3(1.2f, 1.2f) : Vector3.one;

            trns.SetParent(targetParent);
            trns.SetAsLastSibling();
            trns.position = targetParent.position;
            animation.Play(() => trns.SetParent(m_parentRenderer));
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
        public Transform target;
        public int value;
        public bool isCritical;
        public bool isAlliance;
    }
}
