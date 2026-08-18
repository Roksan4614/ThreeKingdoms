using TMPro;
using UnityEngine;

namespace Rev9.Tournament
{
    public class TournamentHeroInfo_Board_Slot : MonoBehaviour, IValidatable
    {
        CharacterComponent m_characeter;
        string m_name;

        public int rank { get; private set; }
        public float totalDamage { get; private set; }

        public CharacterComponent SetHeroData(HeroInfoData _heroData, bool _isMe)
        {
            m_characeter = (Scene_Tournament.instance as Scene_Tournament).GetCharacter(_heroData.key, _isMe);

            m_element.icon.SetProfileData(0, _heroData.skin);

            m_name = _heroData.name;
            SetDealInfo(0);

            return m_characeter;
        }

        public bool SetRank(int _rank)
        {
            bool isUpdated = rank != _rank;
            rank = _rank;
            return isUpdated;
        }

        public void SetDealInfo(float _damage)
        {
            totalDamage += _damage;
            m_element.txtInfo.text = $"{m_name}\n<color=#000000><size=150%>{totalDamage.ToString("#,0")}";
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
                icon = _transform.GetComponent<ProfileIconCompoent>("Icon");
                txtInfo = _transform.GetComponent<TextMeshProUGUI>("txt_info");
            }
        }
        #endregion VALIDATE

    }
}