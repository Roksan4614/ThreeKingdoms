using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillIndicatorComponent : MonoBehaviour, IValidatable
{
    public float speed = 0.001f;

    private void Update()
    {
        if (Input.anyKey)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                m_element.img.fillAmount += speed;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                m_element.img.fillAmount -= speed;
            }

            m_element.side.localRotation = Quaternion.Euler(0, 0, 360 * m_element.img.fillAmount);
            transform.localRotation = Quaternion.Euler(0, 0, 180 * m_element.img.fillAmount);
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public RectTransform rt;
        public Image img;
        public Transform side;
        public void Initialize(Transform _transform)
        {
            rt = (RectTransform)_transform;
            img = _transform.GetComponent<Image>("Image");
            side = img.transform.Find("side");
        }
    }
    #endregion VALIDATA
}
