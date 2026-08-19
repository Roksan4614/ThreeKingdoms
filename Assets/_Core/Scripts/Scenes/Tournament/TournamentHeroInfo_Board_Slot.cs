using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using TMPro;
using UnityEngine;

namespace Rev9.Tournament
{
    public class TournamentHeroInfo_Board_Slot : MonoBehaviour, IValidatable
    {
        CharacterComponent m_characeter;
        string m_name;

        public int rank { get; private set; }
        public long totalDamage { get; private set; }
        public string key => m_characeter.info.key;
        public long hp => m_characeter.stat.health;

        public CharacterComponent SetHeroData(HeroInfoData _heroData, bool _isMe)
        {
            m_characeter = (Scene_Tournament.instance as Scene_Tournament).GetCharacter(_heroData.key, _isMe);

            m_element.icon.SetProfileData(0, _heroData.skin);

            m_name = _heroData.name;
            totalDamage = 0;
            AddDealInfo(0, false);
            rank = transform.GetSiblingIndex();

            return m_characeter;
        }

        public void StartBattle()
        {
            SkillCooldownAsync().Forget();
        }

        CancellationTokenSource m_ctsCooldown;
        async UniTask SkillCooldownAsync()
        {
            m_ctsCooldown = m_ctsCooldown.ReleaseCTS(true);
            var token = m_ctsCooldown.Token;

            var stat = m_characeter.stat;

            var startTime = Time.time;
            var endTime = TableManager.hero.Get(stat.key).skillCooltime * (1 - stat.cooldownRate) + startTime;

            var bar = m_characeter.rtBar_Cooltime;
            bar.parent.gameObject.SetActive(true);
            var width = bar.rect.width;

            while (m_characeter.isLive == true)
            {
                float duration = endTime - startTime;
                float progress = Mathf.Min(1, (Time.time - startTime) / duration);

                var pos = bar.anchoredPosition;
                pos.x = width * progress;
                bar.anchoredPosition = pos;

                if (progress == 1)
                {
                    await UniTask.WaitUntil(() => m_characeter.attack.IsValidUseSkill());

                    await m_characeter.attack.UseSkillAsync();

                    startTime = Time.time;
                    endTime = TableManager.hero.Get(stat.key).skillCooltime * (1 - stat.cooldownRate) + startTime;
                }

                await UniTask.NextFrame(token);
            }
            bar.parent.gameObject.SetActive(false);
        }

        public bool SetRank(int _rank)
        {
            bool isUpdated = rank != _rank;
            rank = _rank;
            return isUpdated;
        }

        Tween m_tween;
        public void AddDealInfo(long _damage, bool _isTween)
        {
            var prev = totalDamage;
            totalDamage += _damage;

            if (_isTween)
            {
                if (m_tween != null)
                    m_tween.Kill();

                m_tween = DOTween.To(() => prev,
                    _result =>
                    {
                        m_element.txtInfo.text = $"{m_name}\n<color=#000000><size=150%>{_result.ToString("#,0")}";
                    },
                    totalDamage, 0.1f);
                //.OnComplete(() => m_element.txtInfo.text = $"{m_name}\n<color=#000000><size=150%>{totalDamage.ToString("#,0")}");

                m_element.panel.localScale = Vector3.one;
                m_element.panel.transform.DOPunchScale(Vector3.one * .025f, 0.05f);
            }
            else
            {
                m_element.txtInfo.text = $"{m_name}\n<color=#000000><size=150%>{totalDamage.ToString("#,0")}";
            }
        }

        public void SetResult(long _teamTotalDamage)
        {
            m_ctsCooldown = m_ctsCooldown.ReleaseCTS();
            m_tween.Kill();
            m_element.txtInfo.text = $"{m_name} ({totalDamage / (double)_teamTotalDamage * 100: 0.#0}%)\n<color=#000000><size=150%>{totalDamage.ToString("#,0")}";
        }

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        [SerializeField, HideInInspector]
        //[SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public ProfileIconCompoent icon;
            public TextMeshProUGUI txtInfo;
            public void Initialize(Transform _transform)
            {
                icon = _transform.GetComponent<ProfileIconCompoent>("Panel/Icon");
                txtInfo = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_info");
            }
            public Transform panel => icon.transform.parent;
        }
        #endregion VALIDATE

    }
}