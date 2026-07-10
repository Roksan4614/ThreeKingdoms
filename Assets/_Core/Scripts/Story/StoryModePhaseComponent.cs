using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StoryModePhaseComponent : MonoBehaviour
{
    public Dictionary<string, CharacterComponent> heroes = new();
    public Dictionary<string, Character_Enemy> enemies = new();

    public CharacterComponent GetHero(CharacterName _name)
    {
        var key = _name.ToString();
        if (heroes.ContainsKey(key))
            return heroes[key];
        if (enemies.ContainsKey(key))
            return enemies[key];

        return null;
    }

    string m_keyMainHero;
    private void Start()
    {
        var pHero = transform.Find("Hero");
        for (int i = 0; i < pHero.childCount; i++)
        {
            var hero = pHero.GetChild(i).GetComponent<CharacterComponent>();
            hero.SetHeroData_StoryModeMain(hero.name, FactionType.Alliance);

            if (i == 0)
            {
                m_keyMainHero = hero.name;
                Signal.instance.ConnectMainHero.Emit(hero);
            }

            heroes.Add(hero.name.Replace(" ",""), hero);
        }
        TeamManager.instance.InitializeAsync_StoryMode(heroes.Values.ToArray()).Forget();

        StageManager.instance.ClearEnemyList();
        var pEnemy = transform.Find("Enemy");
        for (int i = 0; i < pEnemy.childCount; i++)
        {
            var enemy = pEnemy.GetChild(i).GetComponent<Character_Enemy>();
            enemy.SetHeroData_StoryModeMain(enemy.name, FactionType.Enemy);
            enemy.SetBuffStat(0.5f);
            enemies.Add(enemy.name.Replace(" ", ""), enemy);

            StageManager.instance.AddEnemyList(enemy);
        }
    }

    public CharacterComponent mainHero => heroes.Count > 0 ? heroes[m_keyMainHero] : null;

    public void SetPlay(CharacterAnimType _animType, FactionType _factionType)
    {
        if (_factionType == FactionType.NONE || _factionType == FactionType.Alliance)
            foreach (var h in heroes)
                h.Value.anim.Play(_animType);

        if (_factionType == FactionType.NONE || _factionType == FactionType.Enemy)
            foreach (var h in enemies)
                h.Value.anim.Play(_animType);
    }
}
