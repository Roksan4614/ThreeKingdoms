using System;
using TMPro;
using UnityEngine;

public class RewardWorker : Singleton<RewardWorker>, IValidatable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;

    [Serializable]
    public struct ElementData
    {
        public void Initialize(Transform _transform)
        {
        }
    }
}
