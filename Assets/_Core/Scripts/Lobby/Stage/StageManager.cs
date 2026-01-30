using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Unity.VisualScripting;
using UnityEngine;

public class StageManager : MonoSingleton<StageManager>
{
    Transform m_chapter;

    LoadData_Stage m_loadData;

    List<CharacterComponent> m_enemieList = new();

    int m_indexLayerEnemy;
    protected override void OnAwake()
    {
        m_chapter = transform.Find("Chapter");
        m_indexLayerEnemy = LayerMask.NameToLayer("Enemy");

        m_loadData = PPWorker.Get<LoadData_Stage>(PlayerPrefsType.CHAPTER_STAGE_INFO);
    }

    public void StartStage()
    {
        //StartCoroutine(DoStartStage());
    }

    IEnumerator DoStartStage()
    {
#if UNITY_EDITOR
        m_chapter.name = $"Chapter_{m_loadData.chapterId}";
#endif

        // TODO: 어드레서블에서 가져오는 건 후에 하자. 일단 구현만. 26.01.23
        var stage = m_chapter.GetChild(0); // 임시

        //while (true)
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

                m_enemieList.Clear();
                for (int i = 0; i < phase.childCount; i++)
                {
                    var e = phase.GetChild(i).GetComponent<CharacterComponent>();
                    if (e != null)
                    {
                        e.gameObject.layer = m_indexLayerEnemy;
                        m_enemieList.Add(e);
                    }
                }

                // 위치 세팅한다
                var posMainHero = TeamManager.instance.mainHero.transform.position;
                var pos = phase.position;
                pos.x = posMainHero.x;
                phase.position = pos;

                bool isFlip = posMainHero.x > 0;
                if (isFlip == phase.localScale.x > 0)
                {
                    var scale = phase.localScale;
                    scale.x *= -1;
                    phase.localScale = scale;
                }
                // 위치 세팅한다


                var prevPosition = m_enemieList.Select(x => x.transform.position).ToList();

                foreach (var e in m_enemieList)
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

                TeamManager.instance.mainHero.move.SetFlip(isFlip == false);

                TeamManager.instance.RepositionToMain();

                // START PHASE
                TeamManager.instance.SetState(CharacterStateType.SearchEnemy);

                SetState(CharacterStateType.Wait);

                // 클리어 할 때까지 대기
                bool isClear = false;
                while (isClear == false)
                {
                    isClear = true;
                    foreach (var e in m_enemieList)
                    {
                        if (e.isLive)
                        {
                            isClear = false;
                            break;
                        }
                    }
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(1f);

                phase.gameObject.SetActive(false);

                // 클리어하면 원상 복구 시킨다.
                for (int i = 0; i < m_enemieList.Count; i++)
                {
                    var e = m_enemieList[i].transform;
                    e.SetParent(phase);
                    e.position = prevPosition[i];
                }
            }

            TeamManager.instance.mainHero.move.SetFlip(true);
            TeamManager.instance.RepositionToMain();
            TeamManager.instance.SetState(CharacterStateType.Wait);

            // CLEAR STAGE
            yield return null;
        }
    }


    public IReadOnlyList<CharacterComponent> enemyList => m_enemieList.Where(_x => _x.isLive).ToList();

    public Vector3 centerPosition
    {
        get
        {
            var enemies = enemyList;
            Vector3 sum = Vector3.zero;

            for (int i = 0; i < enemies.Count; i++)
                sum += enemies[i].transform.position;

            return sum / enemies.Count;
        }
    }

    public CharacterComponent GetNearestEnemy(Vector3 _position)
    {
        CharacterComponent result = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < m_enemieList.Count; i++)
        {
            var enemy = m_enemieList[i];

            if (enemy.isLive == false)
                continue;

            float sqrDist = (enemy.transform.position - _position).sqrMagnitude;
            if (sqrDist < minDist)
            {
                minDist = sqrDist;
                result = enemy;
            }
        }

        return result;
    }

    public void SetState(CharacterStateType _stateType)
    {
        foreach (var enemy in m_enemieList)
            enemy.SetState(_stateType);
    }
}

public struct LoadData_Stage
{
    public int level;
    public int chapterId;
    public int stageId;
    public bool isBossWait;
}