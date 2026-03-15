using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class SceneBase : Singleton<SceneBase>, IValidatable
{
    protected override void Awake()
    {
        if (Configure.instance.isBooted == false)
        {
            SceneManager.LoadScene("00_Boot");
            return;
        }

        base.Awake();
        m_elementBase.canvas.worldCamera = CameraManager.instance.main;
    }
    public Canvas canvas => m_elementBase.canvas;

    public virtual void OnManualValidate() { m_elementBase.Initialize(transform); }

    [SerializeField]
    protected ElementBaseData m_elementBase;

    [Serializable]
    protected struct ElementBaseData
    {
        [SerializeField]
        Canvas m_canvas;
        public Canvas canvas => m_canvas;

        public void Initialize(Transform _trnsform)
        {
            m_canvas = _trnsform.GetComponent<Canvas>("Canvas");
        }
    }
}
