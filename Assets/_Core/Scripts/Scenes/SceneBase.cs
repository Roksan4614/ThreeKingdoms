using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class SceneBase : Singleton<SceneBase>, IValidatable
{
    public bool isReady { get; protected set; }

    protected override void Awake()
    {
#if UNITY_EDITOR

        if (Configure.instance.isBooted == false)
        {
            SceneManager.LoadScene("00_Boot");
            return;
        }
#endif

        base.Awake();
        m_elementBase.canvas.worldCamera = CameraManager.instance.main;
    }

    public Canvas canvas => m_elementBase.canvas;

    public virtual void OnManualValidate() { m_elementBase.Initialize(transform); }

    [SerializeField, HideInInspector]
    protected ElementBaseData m_elementBase;

    [Serializable]
    protected struct ElementBaseData
    {
        [SerializeField]
        Canvas m_canvas;
        public Canvas canvas => m_canvas;

        public void Initialize(Transform _transform)
        {
            m_canvas = _transform.GetComponent<Canvas>("Canvas");
        }
    }
}
