using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class HeroInfoComponent : MonoBehaviour, IValidatable
{
    CharacterComponent m_hero;

    public string key => isActive ? m_hero.data.key : "";

    CooltimeData m_cooltime_Revive;
    CooltimeData m_cooltime_Skill;

    private void Awake()
    {
        m_element.button.onClick.AddListener(OnButton_UseSkill);

        m_element.startPosition.gameObject.SetActive(false);
    }

    public bool isActive => m_hero != null;

    public void OnButton_UseSkill()
    {
        if (m_statusSkill == StatusType.Valid &&
            m_hero.attack.IsValidUseSkill())
        {
            m_statusSkill = StatusType.Success;
        }
    }

    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }

    public async void SetHeroInfo(CharacterComponent _hero)
    {
        m_hero = _hero;

        var key = DataManager.userInfo.GetHeroInfoData(_hero.data.key).skin;

        int countDestroy = 0;
        for (int i = 0; i < m_element.icon.childCount; i++)
        {
            if (m_element.icon.GetChild(i).name.Contains(key))
                m_element.icon.GetChild(i).AutoResizeParent(true);
            else
            {
                Destroy(m_element.icon.GetChild(i).gameObject);
                countDestroy++;
            }
        }

        if (m_element.icon.childCount == countDestroy)
        {
            m_element.icon.gameObject.SetActive(false);
            var prefab = await AddressableManager.instance.GetHeroIconAsync(key)
                .AttachExternalCancellation(destroyCancellationToken);

            if (prefab != null)
            {
                Instantiate(prefab, m_element.icon)
                    .AutoResizeParent()
                    .name = key;
            }
        }

        m_element.icon.gameObject.SetActive(true);
        m_element.Outline.gameObject.SetActive(true);

        m_element.rtBar_Cooltime.parent.gameObject.SetActive(true);
        m_element.rtBar_HP.parent.gameObject.SetActive(true);

        UpdateHP();
    }

    public void Disable()
    {
        m_element.icon.gameObject.SetActive(false);
        m_element.Outline.gameObject.SetActive(false);
        m_element.objOnSkill.SetActive(false);

        m_element.rtBar_Cooltime.parent.gameObject.SetActive(false);
        m_element.rtBar_HP.parent.gameObject.SetActive(false);

        StopAllCoroutines();
        m_coCooltimeSkill = null;
        StopRespawn();

        m_hero = null;
    }

    public void StartStage()
    {
        if (isActive)
        {
            UpdateHP();
            StopRespawn();
            StartCooldownSkill();
        }
    }

    CancellationTokenSource m_ctsRespawn;
    Tween m_tweenHP;

    public void StopRespawn()
    {
        if (m_ctsRespawn != null)
            m_ctsRespawn.Cancel();

        m_element.txtRespawnTimer.text = "";
        m_element.imgRespawn.gameObject.SetActive(false);
    }

    public void UpdateHP()
    {
        var data = m_hero.data;

        var bar = m_element.rtBar_HP;
        float progress = data.health / (float)data.healthMax;
        var targetX = bar.rect.width * progress - bar.rect.width;

        m_tweenHP?.Kill();

        if (data.health == 0)
        {
            if (TeamManager.instance.IsAllDie() == false)
            {
                bar.DOAnchorPosX(targetX, 0.1f);
                if (m_ctsRespawn != null)
                {
                    m_ctsRespawn.Cancel();
                    m_ctsRespawn.Dispose();
                }
                m_ctsRespawn = new();
                RespawnAsync(bar, 30).Forget();
            }
        }
        else
            m_tweenHP = bar.DOAnchorPosX(targetX, 0.1f);
    }

    public async UniTask RespawnAsync(RectTransform _bar, float _duration)
    {
        m_cooltime_Revive.startTime = Time.realtimeSinceStartup;
        m_cooltime_Revive.endTime = m_cooltime_Revive.startTime + _duration;

        m_element.imgRespawn.gameObject.SetActive(true);
        var prevAlpha = m_element.imgRespawn.color.a;

        //m_element.txtRespawnTimer = transform.GetComponent<TextMeshProUGUI>("Panel/txt_cooltime");

        var width = _bar.rect.width;
        var pos = Vector2.zero;
        pos.x = -width;

        m_element.objOnSkill.SetActive(false);

        while (Time.realtimeSinceStartup < m_cooltime_Revive.endTime)
        {
            float progress = (Time.realtimeSinceStartup - m_cooltime_Revive.startTime) / (m_cooltime_Revive.endTime - m_cooltime_Revive.startTime);

            pos.x = width * progress - width;
            _bar.anchoredPosition = pos;

            m_element.imgRespawn.fillAmount = 1 - progress;

            var remainTime = m_cooltime_Revive.endTime - Time.realtimeSinceStartup;
            m_element.txtRespawnTimer.text = remainTime >= 10 ? Math.Truncate(remainTime).ToString() :
                    (Math.Truncate(remainTime * 10) / 10).ToString("0.0");

            await UniTask.WaitForEndOfFrame(this, m_ctsRespawn.Token);
        }

        transform.GetComponent<ParticleSystem>("Panel/Effect_Respawn").Play();

        m_element.objOnSkill.SetActive(m_statusSkill == StatusType.Valid);

        m_element.txtRespawnTimer.text = "";
        m_element.imgRespawn.fillAmount = 0;
        m_element.imgRespawn.transform.DOScale(Vector3.one * 3, 0.1f);
        m_element.imgRespawn.DOFade(0f, 0.1f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            m_element.imgRespawn.transform.localScale = Vector3.one;
            Utils.SetObjectAlpha(m_element.imgRespawn.gameObject, prevAlpha, false);
            m_element.imgRespawn.gameObject.SetActive(false);
        });

        _bar.anchoredPosition = Vector2.zero;
        TeamManager.instance.SetRespawn(m_hero.teamPosition);

        m_ctsRespawn.Dispose();
        m_ctsRespawn = null;
    }

    public void ApplyRespawnReduction(float _percent)
    {
        m_cooltime_Revive.endTime -= (m_cooltime_Revive.endTime - Time.realtimeSinceStartup) * _percent;
    }

    public void StartCooldownSkill()
    {
        if (m_coCooltimeSkill != null)
            StopCoroutine(m_coCooltimeSkill);

        m_coCooltimeSkill = StartCoroutine(DoCooldownSkill());
    }

    Coroutine m_coCooltimeSkill;
    StatusType m_statusSkill = StatusType.Wait;

    public IEnumerator DoCooldownSkill()
    {
        m_element.objOnSkill.SetActive(false);

        var data = m_hero.data;
        m_cooltime_Skill.startTime = Time.realtimeSinceStartup;
        m_cooltime_Skill.endTime = data.skillCooldown * data.skillCooldownRate + m_cooltime_Skill.startTime;

        var addTime = data.percent_startCooldownRate * data.skillCooldown;
        var bar = m_element.rtBar_Cooltime;
        bar.gameObject.SetActive(true);
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
                m_cooltime_Skill.endTime = data.skillCooldown * data.skillCooldownRate + m_cooltime_Skill.startTime;
                addTime = 0;
                dieTime = -1;
            }

            // 퍼센트 구하기!!
            float duration = m_cooltime_Skill.endTime - m_cooltime_Skill.startTime;
            float progress = (Time.realtimeSinceStartup - m_cooltime_Skill.startTime + addTime) / duration;

            // 바 이동하기!!
            var pos = bar.anchoredPosition;
            pos.x = width * progress;
            bar.anchoredPosition = pos;

            if (m_hero.isMain == true)
                ControllerManager.instance.UpdateColltime_Skill(duration, progress);

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

                    if (DataManager.option.isAutoSkill)
                        OnButton_UseSkill();

                    yield return null;
                }

                // 스킬 사용하기!!
                m_element.objOnSkill.SetActive(false);

                if (m_statusSkill == StatusType.Success)
                    yield return m_hero.attack.UseSkillAsync().ToCoroutine();

                m_statusSkill = StatusType.Wait;
                m_cooltime_Skill.startTime = Time.realtimeSinceStartup;
                m_cooltime_Skill.endTime = data.skillCooldown * data.skillCooldownRate + m_cooltime_Skill.startTime;
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
        public Button button;

        public Transform icon;
        public GameObject objOnSkill;
        public RectTransform rtBar_Cooltime;
        public RectTransform rtBar_HP;

        public Image imgRespawn;
        public TextMeshProUGUI txtRespawnTimer;
        public GameObject Outline;

        public Transform startPosition;

        public Transform panel => icon.parent;

        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<Button>();

            icon = _transform.Find("Panel/Icon");
            objOnSkill = _transform.Find("Panel/OnSkill").gameObject;
            rtBar_HP = _transform.GetComponent<RectTransform>("HP/img_bar");
            rtBar_Cooltime = _transform.GetComponent<RectTransform>("Cooltime/img_bar");
            Outline = panel.Find("Outline").gameObject;

            imgRespawn = _transform.GetComponent<Image>("Panel/img_respawn");
            txtRespawnTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_cooltime");

            startPosition = panel.Find("StartPosition");
        }
    }
}
