using TMPro;
using UnityEngine;

namespace Rev9.Tournament
{
    public class TournamentHeroInfoComponent : MonoBehaviour, IValidatable
    {
        float m_hp;
        float m_hpMax;

        public void Initialize(TournamentRankerUserData _userData, bool _isMe)
        {
            m_element.profile.SetProfileData(_userData.info.indexProfile, _userData.info.skin);
            m_element.txtNickname.text = _userData.info.nickname;

            m_element.tierPoint.text = _userData.info.point.AmountKMBT(_isMBT: true);
            m_element.power.text = _userData.info.power.AmountKMBT(_isMBT: true);

            var board = transform.Find("Board");
            for (int i = 1; i < _userData.batchData.heroes.Count; i++)
                Instantiate(board.GetChild(0), board);

            for (int i = 0; i < board.childCount; i++)
                m_hpMax += board.GetChild(i).GetComponent<TournamentHeroInfo_Board_Slot>()
                    .SetHeroData(_userData.batchData.heroes[i], _isMe).stat.healthMax;

            m_hp = m_hpMax;

            m_element.gauge.SlotUpdateBossHP((1f, m_hpMax));
        }

        public void TakenDamage(float _damage)
        {
            m_hp -= _damage;
            m_element.gauge.SlotUpdateBossHP((Mathf.Max(0, m_hp / m_hpMax), m_hpMax));
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