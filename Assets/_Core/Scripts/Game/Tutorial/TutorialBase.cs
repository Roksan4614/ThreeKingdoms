using Cysharp.Threading.Tasks;
using System;
using Unity.VisualScripting;
using UnityEngine;

public abstract class TutorialBase : MonoBehaviour, IValidatable
{
    protected virtual void Start()
    {
        transform.position = TeamManager.instance.mainHero.transform.position;
        m_elementBase.canvas.worldCamera = CameraManager.instance.main;

        for (int i = 0; i < m_elementBase.arrows.Length; i++)
            m_elementBase.arrows[i].gameObject.SetActive(false);
    }

    public virtual async UniTask StartAsync(TutorialType _type)
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
        public Canvas canvas;

        public Character_Enemy[] enemy;
        public CharacterComponent[] hero;
        public TutorialArrowComponent[] arrows;
        public void Initialize(Transform _transform)
        {
            enemy = _transform.Find("Enemy").GetComponentsInChildren<Character_Enemy>();
            hero = _transform.Find("Hero").GetComponentsInChildren<CharacterComponent>();

            canvas = _transform.GetComponent<Canvas>("Canvas");
            arrows = canvas.transform.Find("Arrow").GetComponentsInChildren<TutorialArrowComponent>();
        }
    }
}
