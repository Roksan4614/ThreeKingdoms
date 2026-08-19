using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Tournament
{
    public class TournamentHeroInfoComponent : MonoBehaviour, IValidatable
    {
        bool m_isMe;

        long m_hp;
        long m_hpMax;

        List<TournamentHeroInfo_Board_Slot> m_slot = new();
        List<float> m_slotPosY = new();
        public long totalDamage => m_hpMax - m_hp;

        public float percentHP => (float)(m_hp / (double)m_hpMax);

        public void Initialize(TournamentRankerUserData _userData, bool _isMe)
        {
            m_isMe = _isMe;

            m_element.profile.SetProfileData(_userData.info.indexProfile, _userData.info.skin);
            m_element.txtNickname.text = _userData.info.nickname;

            m_element.tierPoint.text = _userData.info.point.AmountKMBT(_isMBT: true);
            m_element.power.text = _userData.batchData.totalPower.AmountKMBT(_isMBT: true);

            var board = transform.Find("Board");
            for (int i = 1; i < _userData.batchData.heroes.Count; i++)
                Instantiate(board.GetChild(0), board);

            for (int i = 0; i < board.childCount; i++)
            {
                m_slot.Add(board.GetChild(i).GetComponent<TournamentHeroInfo_Board_Slot>());
                m_hpMax += m_slot[i].SetHeroData(_userData.batchData.heroes[i], _isMe).stat.healthMax;
            }

            m_hp = m_hpMax;

            board.ForceRebuildLayout();
            board.GetComponent<VerticalLayoutGroup>().enabled = false;

            for (int i = 0; i < m_slot.Count; i++)
                m_slotPosY.Add(m_slot[i].transform.localPosition.y);

            m_element.gauge.SlotUpdateBossHP((1f, m_hpMax));
        }

        public void TakenDamage(long _damage)
        {
            m_hp -= _damage;

            if (m_hp <= 0)
            {
                m_hp = 0;
                TournamentWorker.instance.Finished();
            }

            m_element.gauge.SlotUpdateBossHP((Mathf.Max(0, m_hp / (float)m_hpMax), m_hpMax));
        }

        public void SetRankData(CharacterComponent _attacker, long _damage)
        {
            m_slot.Find(x => x.key == _attacker.info.key)
                .AddDealInfo(_damage, true);

            m_slot = m_slot.SortByDescending(x => x.totalDamage);
            for (int i = 0; i < m_slot.Count; i++)
            {
                var slot = m_slot[i];

                // ·©Å©°¡ ¹Ù²î¾ú´Ù¸é,
                if (slot.SetRank(i))
                {
                    slot.transform.DOKill();
                    slot.transform.DOLocalMoveY(m_slotPosY[slot.rank], 0.1f);
                }
            }
        }

        public void StartBattle()
        {
            foreach (var slot in m_slot)
                slot.StartBattle();
        }

        public void SetResult(long _teamTotalDamage)
        {
            foreach (var slot in m_slot)
                slot.SetResult();
        }

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        [SerializeField, HideInInspector]
        //[SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public InfoStage_Boss gauge;
            public ProfileIconCompoent profile;

            public UITierPointHelper tierPoint;
            public UIPowerHelper power;

            public TextMeshProUGUI txtNickname;

            public void Initialize(Transform _transform)
            {
                gauge = _transform.GetComponent<InfoStage_Boss>("Gauge/Panel");
                profile = _transform.GetComponent<ProfileIconCompoent>("Slot_Profile");
                tierPoint = _transform.GetComponent<UITierPointHelper>("TierPoint");
                power = _transform.GetComponent<UIPowerHelper>("Power");
                txtNickname = _transform.GetComponent<TextMeshProUGUI>("txt_nickname");
            }
        }
        #endregion VALIDATE

    }
}