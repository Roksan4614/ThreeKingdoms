using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewHeroComponent : MonoBehaviour, IValidatable
{
    public void Show()
    {
        Play("In");
        gameObject.SetActive(true);

        var scale = transform.localScale;
        if (UnityEngine.Random.value > .5f != scale.x > 0)
            scale.x *= -1;

        transform.localScale = scale;
    }

    public async UniTask OutAsync()
    {
        Play("Out");
        await UniTask.WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }

    public void Play(string _key)
    {
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
