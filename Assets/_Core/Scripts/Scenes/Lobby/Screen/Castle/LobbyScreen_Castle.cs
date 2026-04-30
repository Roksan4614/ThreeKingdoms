using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreen_Castle : LobbyScreen_Base
{
    Dictionary<CastleObjectType, Vector2> m_dbPosObject = new();



    protected override void Awake()
    {
        base.Awake();

        for (var i = CastleObjectType.NONE + 1; i < CastleObjectType.MAX; i++)
            m_dbPosObject.Add(i, m_element.objectPosition[(int)i]);


    }

    void OnButton_Object(CastleObjectType _objectType)
    {

    }



    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public List<Vector2> objectPosition;

        public ScrollRect scroll;

        public Transform pButtons;
        public ButtonHelper[] btnObject;
        public void Initialize(Transform _trnsform)
        {
            scroll = _trnsform.GetComponent<ScrollRect>("Panel/Scroll");

            pButtons = scroll.content.Find("Buttons");
            btnObject = pButtons.GetComponentsInChildren<ButtonHelper>();
        }
    }
    #endregion VALIDATE
}

public enum CastleObjectType
{
    NONE = -1,

    Palace,
    Market,
    Farm,
    Office,
    Merchant,
    Gate,
    MAX
}