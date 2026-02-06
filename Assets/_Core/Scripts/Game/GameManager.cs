using System.Collections;
using System.Linq;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    IEnumerator Start()
    {
        // TODO: 현재 등록된 장수들 로드해야 함
        var heros = MapManager.instance.parentHero.GetComponentsInChildren<CharacterComponent>();
        TeamManager.instance.SetTeamPosition(heros.ToList());
        StageManager.instance.StartStage();

        yield return null;
    }
}
