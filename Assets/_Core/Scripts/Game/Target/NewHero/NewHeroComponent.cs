using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewHeroComponent : MonoBehaviour, IValidatable
{

    public void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            Play("In");
        if (Input.GetKey(KeyCode.RightArrow))
            Play("Out");
        if (Input.GetKey(KeyCode.DownArrow))
            Play("Idle");
    }

    public void Play(string _key)
    {
        //m_animator.Play(_animType.ToString(), _layerIndex, 0);
        m_element.animator.CrossFade(_key, 0, 0, 0);
    }

    public void EventDashStart() => m_element.dash.SetActive(true);

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Animator animator;
        public GameObject dash;

        public void Initialize(Transform _transform)
        {
            animator = _transform.GetComponent<Animator>();
            dash = _transform.Find("Dash").gameObject;
        }
    }
    #endregion VALIDATA
}
