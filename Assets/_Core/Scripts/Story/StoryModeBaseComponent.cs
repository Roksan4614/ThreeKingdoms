using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public abstract class StoryModeBaseComponent : MonoBehaviour, IValidatable
{
    protected Dictionary<string, CharacterComponent> m_heroes = new();
    protected Dictionary<string, CharacterComponent> m_enemies = new();

    public Queue<TableStringData> m_queTalk = new();

    string m_keyMainHero;
    protected virtual void Start()
    {
        TeamManager.instance.SetHeroInfoHide(true, false);
        HeroNavigationComponent.instance.gameObject.SetActive(false);

        for (int i = 0; i < m_elementBase.pHero.childCount; i++)
        {
            var hero = m_elementBase.pHero.GetChild(i).GetComponent<CharacterComponent>();
            hero.SetHeroData_StoryModeMain(hero.name);

            if (i == 0)
            {
                m_keyMainHero = hero.name;
                Signal.instance.ConnectMainHero.Emit(hero);
            }

            m_heroes.Add(hero.name, hero);
        }

        for (int i = 0; i < m_elementBase.pEnemy.childCount; i++)
        {
            var enemy = m_elementBase.pEnemy.GetChild(i).GetComponent<CharacterComponent>();
            enemy.SetHeroData_StoryModeMain(enemy.name);
            m_enemies.Add(enemy.name, enemy);
        }

        m_queTalk = TableManager.scenarioTalk.GetTalk(DataManager.storyMode.curNodeKey, true);
    }

    public abstract UniTask StartAsync();

    public CharacterComponent mainHero => m_keyMainHero.IsActive() ? m_heroes[m_keyMainHero] : null;

    #region VALIDATE
    public virtual void OnManualValidate() => m_elementBase.Initialize(transform);

    [SerializeField, HideInInspector]
    protected ElementData m_elementBase;

    [System.Serializable]
    protected struct ElementData
    {
        public Transform pHero, pEnemy;

        public void Initialize(Transform _transform)
        {
            pHero = _transform.Find("Hero");
            pEnemy = _transform.Find("Enemy");
        }
    }
    #endregion VALIDATE

}
