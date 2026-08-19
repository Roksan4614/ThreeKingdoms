using UnityEngine;

namespace Rev9.Tournament
{
    public class TournamentHeroInfoManager : Singleton<TournamentHeroInfoManager>, IValidatable
    {
        private void Start()
        {
            Signal.instance.UpdateHP.connect = SlotUpdateHp;
        }

        public void Initialize()
        {
            m_element.me.Initialize(new()
            {
                batchData = TournamentWorker.data.teamAttack,
                info = TournamentWorker.instance.rankData
            }, true);

            m_element.other.Initialize(TournamentWorker.instance.enterUserData, false);
        }

        public void StartBattle()
        {
            m_element.me.StartBattle();
            m_element.other.StartBattle();
        }

        void SlotUpdateHp((CharacterComponent hero, CharacterComponent attacker, long damage) _data)
        {
            if (TournamentWorker.instance.statusType == TournamentStatusType.Finished)
                return;

            var defenceSlot = _data.hero.factionType == FactionType.Alliance ? m_element.me : m_element.other;
            defenceSlot.TakenDamage(_data.damage);

            var attackSlot = _data.attacker.factionType == FactionType.Alliance ? m_element.me : m_element.other;
            attackSlot.SetRankData(_data.attacker, _data.damage);
        }

        public void SetResult()
        {
            m_element.me.SetResult(m_element.other.totalDamage);
            m_element.other.SetResult(m_element.me.totalDamage);
        }

        public bool IsWin()
        {
            return m_element.me.percentHP > m_element.other.percentHP;
        }

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        [SerializeField, HideInInspector]
        //[SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public TournamentHeroInfoComponent me;
            public TournamentHeroInfoComponent other;

            public void Initialize(Transform _transform)
            {
                me = _transform.GetComponent<TournamentHeroInfoComponent>("Me");
                other = _transform.GetComponent<TournamentHeroInfoComponent>("Other");
            }
        }
        #endregion VALIDATE
    }
}