using System;
using UnityEngine;
using UnityEngine.UI;

public class PopupSelectRegion_HeroInfo : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void OnManualValidate()
    {

    }

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {

    }
}
