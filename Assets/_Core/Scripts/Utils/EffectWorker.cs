using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EffectWorker : MonoSingleton<EffectWorker>
{
    enum EffectType
    {
        //damage,
        //bounty,
        hit,
        //die,
    }

    [SerializeField]
    AnimationData m_configureDamage;

    TextMeshProUGUI m_baseDamage;
    TextMeshProUGUI m_baseBounty;
    SpriteAnimaion m_baseEffectHit;
    SpriteAnimaion m_baseEffectDie;

    Dictionary<EffectType, List<TextMeshProUGUI>> m_fx_text = new()
    {
        //{ EffectType.damage, new List<TextMeshProUGUI>() },
        //{ EffectType.bounty, new List<TextMeshProUGUI>() },
    };

    Dictionary<EffectType, List<SpriteAnimaion>> m_fx_animation = new()
    {
        { EffectType.hit, new List<SpriteAnimaion>() },
        //{ EffectType.die, new List<SpriteAnimaion>() },
    };

    protected override void OnAwake()
    {
        //m_baseDamage = transform.GetComponent<TextMeshProUGUI>("Panel/txt_damage");
        //m_baseDamage.gameObject.SetActive(false);

        //m_baseBounty = transform.GetComponent<TextMeshProUGUI>("Panel/txt_bounty");
        //m_baseBounty.gameObject.SetActive(false);

        m_baseEffectHit = transform.GetComponent<SpriteAnimaion>("Renderer/DamageHit");
        m_baseEffectHit.gameObject.SetActive(false);

        //m_baseEffectDie = transform.GetComponent<SpriteAnimaion>("Panel/effect_die");
        //m_baseEffectDie.gameObject.SetActive(false);
    }

    public void SlotDamageTakenEffect(EffectData _damageData)
    {
        //if (m_fx_text[EffectType.damage] == null)
        //    return;

        

        // 데미지 표시해주자
        //{
        //    TextMeshProUGUI txtDamage = m_fx_text[EffectType.damage].Find(x => x.gameObject.activeSelf == false);

        //    if (txtDamage == null)
        //    {
        //        txtDamage = Instantiate(m_baseDamage, transform).GetComponent<TextMeshProUGUI>();
        //        m_fx_text[EffectType.damage].Add(txtDamage);
        //    }

        //    txtDamage.text = $"<size={txtDamage.fontSize * (_damageData.isCritical ? 1.3 : 1)}><color=#{(_damageData.value > 0 ? "00A909>+" : _damageData.isCritical ? "DDD800>" : "960000>")}{_damageData.value}</color></size>";
        //    txtDamage.transform.SetParent(parent);
        //    //txtDamage.transform.SetAsLastSibling();

        //    StartCoroutine(DoShowEffectValue(txtDamage.gameObject, _damageData.target.position));
        //}

        // effect 연출해주자
        //if (_damageData.value < 0)
        {
            var parent = _damageData.target.Find("Character/Effect_Renderer");

            SpriteAnimaion animation = m_fx_animation[EffectType.hit].Find(x => x.gameObject.activeSelf == false);
            if (animation == null)
            {
                animation = Instantiate(m_baseEffectHit, transform).GetComponent<SpriteAnimaion>();
                m_fx_animation[EffectType.hit].Add(animation);
            }

            var angle = Vector3.Angle(Vector3.right, _damageData.target.position - _damageData.attacker.position);
            if (_damageData.target.position.y < _damageData.attacker.position.y)
                angle = 360 - angle;
            animation.transform.localEulerAngles = new Vector3(0, 0, angle - 90);

            //var localScale = animation.rt.localScale;
            //localScale.x *= -1;
            //animation.rt.localScale = localScale;

            animation.transform.SetParent(parent);
            //animation.transform.SetAsLastSibling();
            animation.transform.position = parent.position;// + new Vector3(0, -0.3f);
            animation.Play();
        }
    }

    IEnumerator DoShowEffectValue(GameObject _damageObject, Vector3 _posTarget)
    {
        _damageObject.SetActive(true);
        Utils.SetObjectAlpha(_damageObject, 1f);
        _damageObject.transform.position = _posTarget + new Vector3(0, m_configureDamage.posY);

        var rt = _damageObject.GetComponent<RectTransform>();
        yield return rt.DOAnchorPos(rt.anchoredPosition + new Vector2(0, 30f), 0.2f).SetEase(Ease.OutCubic).WaitForCompletion();

        _damageObject.SetActive(false);
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

    IEnumerator DoInstantiateEffect(EffectData _effectData)
    {
        var effect = _effectData.attacker;
        effect.SetParent(transform.Find("World"));
        effect.rotation = Quaternion.identity;

        yield return new WaitUntil(() => effect.gameObject.activeSelf == false);

        effect.SetParent(_effectData.target);
    }

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

    public struct EffectData
    {
        public Transform attacker;
        public Transform target;
        public int value;
        public bool isCritical;
        public bool isDie;
    }

    [Serializable]
    struct AnimationData
    {
        public AnimationCurve curve;
        public AnimationCurve curve_alpha;
        public float duration;
        public float posY;
    }
}
