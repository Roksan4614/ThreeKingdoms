using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public abstract class ScenarioBase : MonoBehaviour, IValidatable
{
    protected virtual void Start()
        => transform.position = TeamManager.instance.mainHero.transform.position;

    public virtual async UniTask StartAsync(string _stageKey)
        => await UniTask.WaitForEndOfFrame();

    public virtual void OnManualValidate()
    {
        m_elementBase.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    protected ElementBaseData m_elementBase;

    [Serializable]
    protected struct ElementBaseData
    {
        public Character_Enemy[] enemy;

        public void Initialize(Transform _transform)
        {
            enemy = _transform.Find("Enemy").GetComponentsInChildren<Character_Enemy>();
        }
    }
}
