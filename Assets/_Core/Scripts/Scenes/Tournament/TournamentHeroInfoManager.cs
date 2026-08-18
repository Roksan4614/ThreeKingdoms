using UnityEngine;

namespace Rev9.Tournament {
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

        void SlotUpdateHp((CharacterComponent hero, CharacterComponent attacker, float damage) _data)
        {
            var slot = _data.hero.factionType == FactionType.Alliance ? m_element.me : m_element.other;

            slot.TakenDamage(_data.damage);
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