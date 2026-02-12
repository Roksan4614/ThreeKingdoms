using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HeroInfoComponent : MonoBehaviour, IValidatable
{
    CharacterComponent m_hero;

    CooltimeData m_cooltime_Revive;
    CooltimeData m_cooltime_Skill;

    private void Awake()
    {
        transform.GetComponent<Button>("Panel").onClick.AddListener(OnButton_UseSkill);
    }

    void OnButton_UseSkill()
    {
        if (m_statusSkill == StatusType.Valid &&
            m_hero.attack.IsValidUseSkill())
        {
            m_statusSkill = StatusType.Success;
        }
    }

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }
#endif

    public async void SetHeroInfo(CharacterComponent _hero)
    {
        m_hero = _hero;

        var key = _hero.data.key;

        int countDestroy = 0;
        for (int i = 0; i < m_element.icon.childCount; i++)
        {
            if (m_element.icon.GetChild(i).name.Contains(key) == false)
            {
                Destroy(m_element.icon.GetChild(i).gameObject);
                countDestroy++;
            }
        }

        if (m_element.icon.childCount == countDestroy)
        {
            m_element.icon.gameObject.SetActive(false);
            var prefab = await AddressableManager.instance.GetHeroIcon(key);

            if (prefab != null)
                Instantiate(prefab, m_element.icon).name = key;
        }

        m_element.icon.gameObject.SetActive(true);

        UpdateHP();
    }

    public void Disable()
    {
        var panel = m_element.icon.parent;

        for (int i = 0; i < panel.childCount; i++)
            panel.GetChild(i).gameObject.SetActive(false);

        transform.Find("HP").gameObject.SetActive(false);
        transform.Find("Cooltime").gameObject.SetActive(false);

        m_hero = null;
    }


    Tween m_tweenHP;
    public void UpdateHP()
    {
        var data = m_hero.data;

        var bar = m_element.rtBar_HP;
        float progress = data.health / (float)data.healthMax;
        var targetX = bar.rect.width * progress - bar.rect.width;

        m_tweenHP?.Kill();

        if (data.health == 0)
            StartCoroutine(DoRespawn(bar, data.duration_respawn));
        else
            m_tweenHP = bar.DOAnchorPosX(targetX, 0.1f);
    }

    public IEnumerator DoRespawn(RectTransform _bar, float _duration)
    {
        m_cooltime_Revive.startTime = Time.realtimeSinceStartup;
        m_cooltime_Revive.endTime = m_cooltime_Revive.startTime + _duration;

        var imgRespawn = transform.GetComponent<Image>("Panel/img_respawn");
        imgRespawn.gameObject.SetActive(true);
        var prevAlpha = imgRespawn.color.a;

        var txtTimer = imgRespawn.transform.GetComponent<Text>("txt_cooltime");

        var width = _bar.rect.width;
        var pos = Vector2.zero;
        pos.x = -width;

        m_element.objOnSkill.SetActive(false);

        while (Time.realtimeSinceStartup < m_cooltime_Revive.endTime)
        {
            float progress = (Time.realtimeSinceStartup - m_cooltime_Revive.startTime) / (m_cooltime_Revive.endTime - m_cooltime_Revive.startTime);

            pos.x = width * progress - width;
            _bar.anchoredPosition = pos;

            imgRespawn.fillAmount = 1 - progress;

            var remainTime = m_cooltime_Revive.endTime - Time.realtimeSinceStartup;
            txtTimer.text = remainTime >= 10 ? Math.Truncate(remainTime).ToString() :
                    (Math.Truncate(remainTime * 10) / 10).ToString("0.0");

            yield return null;
        }

        transform.GetComponent<ParticleSystem>("Panel/Effect_Respawn").Play();

        m_element.objOnSkill.SetActive(m_statusSkill == StatusType.Valid);

        txtTimer.text = "";
        imgRespawn.fillAmount = 0;
        imgRespawn.transform.DOScale(Vector3.one * 3, 0.1f);
        imgRespawn.DOFade(0f, 0.1f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            imgRespawn.transform.localScale = Vector3.one;
            Utils.SetObjectAlpha(imgRespawn.gameObject, prevAlpha, false);
            imgRespawn.gameObject.SetActive(false);
        });

        _bar.anchoredPosition = Vector2.zero;
        TeamManager.instance.SetRespawn(m_hero.teamPosition);
    }

    public void ApplyRespawnReduction(float _percent)
    {
        m_cooltime_Revive.endTime -= (m_cooltime_Revive.endTime - Time.realtimeSinceStartup) * _percent;
    }

    public void StartCooltimeSkill()
    {
        if (m_coCooltimeSkill != null)
            StopCoroutine(m_coCooltimeSkill);

        m_coCooltimeSkill = StartCoroutine(DoCooltimeSkill());
    }

    Coroutine m_coCooltimeSkill;
    StatusType m_statusSkill = StatusType.Wait;

    public IEnumerator DoCooltimeSkill()
    {
        var data = m_hero.data;

        m_cooltime_Skill.startTime = Time.realtimeSinceStartup;
        m_cooltime_Skill.endTime = data.cooltime_skill + m_cooltime_Skill.startTime;

        var addTime = data.percent_startCooltime * data.cooltime_skill;
        var bar = m_element.rtBar_Cooltime;
        var width = bar.rect.width;

        float dieTime = -1;
        m_statusSkill = StatusType.Wait;
        while (true)
        {
            if (m_hero.isLive == false)
            {
                if (dieTime < 0)
                {
                    bar.gameObject.SetActive(false);
                    dieTime = Time.realtimeSinceStartup;
                }
                yield return null;
                continue;
            }
            else if (dieTime > 0)
            {
                bar.gameObject.SetActive(true);
                m_cooltime_Skill.startTime = Time.realtimeSinceStartup;
                m_cooltime_Skill.endTime = data.cooltime_skill + m_cooltime_Skill.startTime;
                addTime = 0;
                dieTime = -1;
            }

            // 퍼센트 구하기!!
            float progress = (Time.realtimeSinceStartup - m_cooltime_Skill.startTime + addTime) / (m_cooltime_Skill.endTime - m_cooltime_Skill.startTime);

            // 바 이동하기!!
            var pos = bar.anchoredPosition;
            pos.x = width * progress;
            bar.anchoredPosition = pos;

            if (progress > 1f)
            {
                m_statusSkill = StatusType.Valid;

                pos.x = width;
                bar.anchoredPosition = pos;

                m_element.objOnSkill.SetActive(true);

                // 버튼 누를 때까지 기다리기!!
                while (m_statusSkill != StatusType.Success)
                {
                    //죽으면 끄기
                    if (m_hero.isLive == false)
                    {
                        m_statusSkill = StatusType.Failed;
                        break;
                    }

                    yield return null;
                }

                // 스킬 사용하기!!
                m_element.objOnSkill.SetActive(false);

                if (m_statusSkill == StatusType.Success)
                    yield return m_hero.attack.DoUseSkill();

                m_statusSkill = StatusType.Wait;
                m_cooltime_Skill.startTime = Time.realtimeSinceStartup;
                m_cooltime_Skill.endTime = data.cooltime_skill + m_cooltime_Skill.startTime;
                addTime = 0;
            }

            yield return null;
        }
    }

    struct CooltimeData
    {
        public float startTime;
        public float endTime;
    }

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform icon;
        public GameObject objOnSkill;
        public RectTransform rtBar_Cooltime;
        public RectTransform rtBar_HP;

        public Transform panel => icon.parent;

        public void Initialize(Transform _transform)
        {
            icon = _transform.Find("Panel/Icon");
            objOnSkill = _transform.Find("Panel/OnSkill").gameObject;
            rtBar_HP = _transform.GetComponent<RectTransform>("HP/img_bar");
            rtBar_Cooltime = _transform.GetComponent<RectTransform>("Cooltime/img_bar");
        }
    }
}
