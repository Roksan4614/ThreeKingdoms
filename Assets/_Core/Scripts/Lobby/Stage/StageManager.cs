using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageManager : MonoSingleton<StageManager>
{
    Transform m_chapter;

    LoadData_Stage m_loadData;

    protected override void OnAwake()
    {
        m_chapter = transform.Find("Chapter");

        m_loadData = PPWorker.Get<LoadData_Stage>(PlayerPrefsType.CHAPTER_STAGE_INFO);
    }

    public void StartStage()
    {
        StartCoroutine(DoStartStage());
    }

    IEnumerator DoStartStage()
    {
#if UNITY_EDITOR
        m_chapter.name = $"Chapter_{m_loadData.chapterId}";
#endif

        // TODO: 어드레서블에서 가져오는 건 후에 하자. 일단 구현만. 26.01.23
        var stage = m_chapter.GetChild(0); // 임시

        while (true)
        {
            var phases = new Queue<Transform>(stage.Find("Phase").Cast<Transform>());

            foreach (var p in phases)
                p.gameObject.SetActive(false);

            while (phases.Count > 0)
            {
                var phase = phases.Dequeue();
                var isLastPhase = phases.Count == 0;

                // 보스 대기중이라면 
                if (m_loadData.isBossWait == true && isLastPhase == true)
                    continue;

                phase.gameObject.SetActive(true);

                List<CharacterComponent> enemies = new();
                for (int i = 0; i < phase.childCount; i++)
                {
                    var e = phase.GetChild(i).GetComponent<CharacterComponent>();
                    if (e != null)
                        enemies.Add(e);
                }

                // 위치 세팅한다
                bool isFlip = false;
                {
                    var posMainHero = TeamManager.instance.mainCharacter.transform.position;
                    var pos = phase.position;
                    pos.x = posMainHero.x;
                    phase.position = pos;

                    // 왼쪽에 있으면 오른쪽에 배치해주자
                    isFlip = posMainHero.x > 0;

                    if (isFlip == phase.localScale.x > 0)
                    {
                        var scale = phase.localScale;
                        scale.x *= -1;
                        phase.localScale = scale;
                    }
                }

                var prevPosition = enemies.Select(x => x.transform.position).ToList();

                foreach (var e in enemies)
                {
                    e.SetFaction(FactionType.Enemy);
                    e.transform.SetParent(MapManager.instance.parentEnemy);

                    if (e.transform.localScale.x < 0)
                    {
                        var scale = e.transform.localScale;
                        scale.x *= -1;
                        e.transform.localScale = scale;
                    }

                    e.move.SetFlip(isFlip);
                    e.SetState(CharacterStateType.Wait);
                }

                // 클리어 할 때까지 대기
                bool isClear = false;
                while (isClear == false)
                {
                    isClear = true;
                    foreach (var e in enemies)
                    {
                        if (e.isLive)
                        {
                            isClear = false;
                            break;
                        }
                    }
                    yield return null;
                }

                // 클리어하면 원상 복구 시킨다.
                for (int i = 0; i < enemies.Count; i++)
                    enemies[i].transform.position = prevPosition[i];

                phase.gameObject.SetActive(false);

                TeamManager.instance.RepositionToMain();

                yield return new WaitForSecondsRealtime(1f);
            }

            // CLEAR STAGE
            yield return null;
        }
    }
}

public struct LoadData_Stage
{
    public int level;
    public int chapterId;
    public int stageId;
    public bool isBossWait;
}