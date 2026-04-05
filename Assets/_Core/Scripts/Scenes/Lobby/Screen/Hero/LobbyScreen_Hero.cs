using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyScreen_Hero : LobbyScreen_Base
{

    protected override void Awake()
    {
        base.Awake();

    }

    protected override bool IsCloseScreen()
    {
        if (m_element.hero.IsCloseScreen())
            return true;

        return false;
    }

    protected override async UniTask CloseAsync()
    {
        await m_element.hero.CloseAsync();

        await base.CloseAsync();
    }

    public override void Close(bool _isTween = true)
    {
        // SaveData
        m_element.hero.SaveDataAsync().Forget();
        base.Close(_isTween);
    }
    public override void OnManualValidate()
    {
        m_element.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;
    [Serializable]
    struct ElementData
    {
        public LobbyScreen_Hero_Hero hero;

        public void Initialize(Transform _transform)
        {
            var panel = _transform.Find("Panel");
            hero = panel.GetComponent<LobbyScreen_Hero_Hero>("Hero");
        }
    }
}