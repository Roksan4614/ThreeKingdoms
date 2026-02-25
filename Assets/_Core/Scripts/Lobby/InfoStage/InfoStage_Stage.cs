using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

public class InfoStage_Stage : MonoBehaviour, IValidatable
{
    public RectTransform rt => (RectTransform)transform;

    public void SetPhase(int _stageIdx)
    {
        gameObject.SetActive(true);

        for (int i = 0; i < m_element.stage.Count; i++)
            m_element.stage[i].SetActive(i < _stageIdx);
    }

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }
#endif

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;

    [Serializable]
    public struct ElementData
    {
        public CanvasGroup cg;

        public List<GameObject> stage;

        public void Initialize(Transform _transform)
        {
            cg = _transform.GetComponent<CanvasGroup>();

            stage = new();
            stage.Add(_transform.Find("stage_1").gameObject);
            stage.Add(_transform.Find("stage_2").gameObject);
            stage.Add(_transform.Find("img_stick_boss").gameObject);
        }
    }
}
