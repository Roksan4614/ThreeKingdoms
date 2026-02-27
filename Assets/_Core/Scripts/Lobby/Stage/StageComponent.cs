using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class StageComponent : MonoBehaviour, IValidatable
{
    public StageManager.LoadData_Stage data { get; private set; }

    struct StageData
    {
        public List<Vector3> enemyLocalPos;
        public CancellationTokenSource ctsClose;

        public StageData CreateNewCTS()
        {
            ctsClose = new();
            return this;
        }
        public StageData ResetCTS()
        {
            if (ctsClose != null)
            {
                ctsClose.Cancel();
                ctsClose.Dispose();
                ctsClose = null;
            }
            return this;
        }
    }

    Dictionary<int, StageData> m_stage = new();

    private void Awake()
    {
        for (int i = 0; i < m_element.phase.childCount; i++)
        {
            StageData stageData = new();
            stageData.enemyLocalPos = m_element.phase.GetChild(i).Cast<Transform>().Select(x => x.localPosition).ToList();
            m_stage.Add(i, stageData);
        }
    }

    public bool IsNow(StageManager.LoadData_Stage _data)
        => data.level == _data.level && data.chapterNumber == _data.chapterNumber && data.stageNumber == _data.stageNumber;
    public void SetData(StageManager.LoadData_Stage _data) => data = _data;

    public CancellationTokenSource StartPhase(int _phaseIdx, bool _isFlip)
    {
        ClosePhase(_phaseIdx);

        var phase = m_element.phase.GetChild(_phaseIdx);
        phase.gameObject.SetActive(true);


        var scalePhase = phase.localScale;
        if (_isFlip == scalePhase.x > 0)
        {
            scalePhase.x *= -1;
            phase.localScale = scalePhase;
        }

        var pos = phase.position;
        pos.x = TeamManager.instance.mainHero.transform.position.x;

        if (DataManager.option.mainTeamPosition == TeamPositionType.Back)
        {
            pos.x -= TeamManager.instance.GetDBTeamPosition(TeamPositionType.Back).x
                * (_isFlip ? -1 : 1);
        }

        phase.position = pos;

        m_stage[_phaseIdx] = m_stage[_phaseIdx].CreateNewCTS();
        return m_stage[_phaseIdx].ctsClose;
    }

    public void ClosePhaseAll()
    {
        for (int i = 0; i < m_element.phase.childCount; i++)
            ClosePhase(i);
    }

    public void ClosePhase(int _phaseIdx)
    {
        m_stage[_phaseIdx] = m_stage[_phaseIdx].ResetCTS();

        var phase = m_element.phase.GetChild(_phaseIdx);

        for (int i = 0; i < phase.childCount; i++)
        {
            var e = phase.GetChild(i);
            e.localPosition = m_stage[_phaseIdx].enemyLocalPos[i];
            var parts = e.Find("Character/Panel/Parts");
            parts.Find("Sub").gameObject.SetActive(true);
            parts.Find("Weapon").gameObject.SetActive(true);
            
        }

        phase.gameObject.SetActive(false);
    }


    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }
    [SerializeField]
    ElementData m_element; public ElementData element => m_element;
    [Serializable]
    public struct ElementData
    {
        public Transform phase;

        public void Initialize(Transform _trnasform)
        {
            phase = _trnasform.Find("Phase");
        }
    }
}