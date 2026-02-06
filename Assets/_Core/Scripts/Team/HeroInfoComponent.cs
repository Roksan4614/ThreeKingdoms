using DG.Tweening;
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class HeroInfoComponent : MonoBehaviour
{
    TeamPositionType m_positionType;

    float m_startTime;
    float m_endTime;

    private void Awake()
    {
        transform.GetComponent<Button>("Panel").onClick.AddListener(OnButton_UseSkill);
    }

    void OnButton_UseSkill()
    {
        if (m_endTime > Time.realtimeSinceStartup)
        {
            ApplyReviceReduction(0.5f);
            return;
        }
        else
        {
            UpdateHP(new Data_Character()
            {
                health = 0,
                healthMax = 1000,
                duration_respawn = 15,
            });
        }

    }


    public void SetHeroInfo(CharacterComponent _hero)
    {
        m_positionType = _hero.teamPosition;

        var panel = transform.Find("Panel");

        var icon = panel.Find("Icon");
        icon.gameObject.SetActive(false);

        AddressableManager.instance.Load_HeroIcon(_hero.name, _icon =>
        {
            icon.gameObject.SetActive(true);

            for (int i = 0; i < icon.childCount; i++)
            {
                if (icon.GetChild(i).name != _icon.name)
                    Destroy(icon.GetChild(i).gameObject);
            }

            Instantiate(_icon, icon).name = _icon.name;
        });

        UpdateHP(_hero.data);
    }

    public void Disable()
    {
        var panel = transform.Find("Panel");

        for (int i = 0; i < panel.childCount; i++)
            panel.GetChild(i).gameObject.SetActive(false);

        transform.Find("HP").gameObject.SetActive(false);
        transform.Find("Cooltime").gameObject.SetActive(false);
    }


    Tween m_tweenHP;
    public void UpdateHP(Data_Character _data)
    {
        var hp = transform.Find("HP");
        hp.gameObject.SetActive(true);

        var bar = hp.GetComponent<RectTransform>("img_bar");
        float progress = _data.health / (float)_data.healthMax;
        var targetX = bar.rect.width * progress - bar.rect.width;

        m_tweenHP?.Kill();

        if (_data.health == 0)
            StartCoroutine(DoRespawn(bar, _data.duration_respawn));
        else
            m_tweenHP = bar.DOAnchorPosX(targetX, 0.1f);
    }

    public IEnumerator DoRespawn(RectTransform _bar, float _duration)
    {
        m_startTime = Time.realtimeSinceStartup;
        m_endTime = m_startTime + _duration;

        var imgRespawn = transform.GetComponent<Image>("Panel/img_respawn");
        imgRespawn.gameObject.SetActive(true);
        var prevAlpha = imgRespawn.color.a;

        var txtTimer = imgRespawn.transform.GetComponent<Text>("txt_cooltime");

        var width = _bar.rect.width;
        var pos = Vector2.zero;
        pos.x = -width;

        while (Time.realtimeSinceStartup < m_endTime)
        {
            float progress = (Time.realtimeSinceStartup - m_startTime) / (m_endTime - m_startTime);

            pos.x = width * progress - width;
            _bar.anchoredPosition = pos;

            imgRespawn.fillAmount = progress;

            var remainTime = m_endTime - Time.realtimeSinceStartup;
            txtTimer.text = remainTime.ToString(remainTime > 10 ? "#0" : "0.0");

            yield return null;
        }

        transform.GetComponent<ParticleSystem>("Panel/Effect_Respawn").Play();

        txtTimer.text = "";
        imgRespawn.fillAmount = 1;
        imgRespawn.transform.DOScale(Vector3.one * 3, 0.1f);
        imgRespawn.DOFade(0f, 0.1f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            imgRespawn.transform.localScale = Vector3.one;
            Utils.SetObjectAlpha(imgRespawn.gameObject, prevAlpha, false);
            imgRespawn.gameObject.SetActive(false);
        });

        _bar.anchoredPosition = Vector2.zero;
        TeamManager.instance.SetRespawn(m_positionType);
    }

    public void ApplyReviceReduction(float _percent)
    {
        m_endTime -= (m_endTime - Time.realtimeSinceStartup) * _percent;
    }
}
