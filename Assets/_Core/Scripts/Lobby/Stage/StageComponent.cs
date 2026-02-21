using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using UnityEditor.Build.Pipeline;
using UnityEngine;

public class StageComponent : MonoBehaviour, IValidatable
{
    public LoadData_Stage data { get; private set; }

    public bool IsNow(LoadData_Stage _data)
        => data.level == _data.level && data.chapterIdx == _data.chapterIdx && data.stageIdx == _data.stageIdx;
    public void SetData(LoadData_Stage _data) => data = _data;

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