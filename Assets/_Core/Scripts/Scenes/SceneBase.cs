using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class SceneBase : MonoBehaviour, IValidatable
{
    protected virtual void Awake()
    {
        if (Configure.instance.isBooted == false)
        {
            SceneManager.LoadScene("00_Boot");
            return;
        }

        m_elementBase.canvas.worldCamera = CameraManager.instance.main;
    }

    public virtual void OnManualValidate() { m_elementBase.Initialize(transform); }

    [SerializeField]
    ElementBaseData m_elementBase;

    [Serializable]
    struct ElementBaseData
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
