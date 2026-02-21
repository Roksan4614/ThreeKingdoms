using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;


public class CharacterComponent : TargetComponent
{
    [SerializeField]
    protected TableHeroData m_data;
    [SerializeField]
    FactionType m_faction;
    [SerializeField]
    HeroInfoData m_info;

    [SerializeField]
    CharacterStateType m_stateType;

    CharacterState m_state;

    Dictionary<CharacterStateType, CharacterState> m_dbState = new();

    public Character_Woker_Anim anim { get; private set; }
    public Character_Woker_Move move { get; private set; }
    public Character_Worker_Attack attack { get; private set; }
    public Character_Worker_Target target { get; private set; }
    public Transform panel => m_element.panel;
    public Rigidbody2D rig => m_element.rig;
    public TableHeroData data => m_data;
    public HeroInfoData info => m_info;
    public override bool isLive => gameObject != null && data.health > 0;
    public bool isMain => m_element.isMain;
    public FactionType factionType => m_faction;
    public TeamPositionType teamPosition { get; private set; } = TeamPositionType.NONE;

    public void SetMain(bool _isMain) => m_element.isMain = _isMain;

    void Awake()
    {
        anim = new(this);// m_element.animator = default;
        move = new(this);
        attack = new(this);
        target = new(this);

        m_dbState.Add(CharacterStateType.Wait, new CharacterState_Wait(this));
        m_dbState.Add(CharacterStateType.SearchEnemy, new CharacterState_SearchEnemy(this));
        m_dbState.Add(CharacterStateType.Battle, new CharacterState_Battle(this));

        SetState(CharacterStateType.Wait);

        if (m_faction == FactionType.Enemy)
        {
            m_data.attackPower /= 2;
            m_data.healthMax = m_data.health /= 2;
        }
    }

#if UNITY_EDITOR
    public override void OnManualValidate()
    {
        m_element.Initialize(transform);

        base.OnManualValidate();
    }
#endif

    public virtual void SetHeroData(string _key)
    {
        m_data = TableManager.hero.GetHeroData(_key);
        m_info = DataManager.userInfo.GetHeroInfoData(_key);
    }

    public void SetFaction(FactionType _factionType) => m_faction = _factionType;

    public void SetTeamPosition(TeamPositionType _teamPosition, Vector3 _position)
    {
        teamPosition = _teamPosition;
        anim.Play(CharacterAnimType.Idle);
        transform.position = _position;
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
        {
            var hero = _collision.transform.parent.GetComponent<CharacterComponent>();
            target.AddTarget(hero);
        }
    }

    private void OnTriggerExit2D(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
        {
            var hero = _collision.transform.parent.GetComponent<CharacterComponent>();
            if (hero == null || target == null)
                return;
            target.RemoveTarget(hero);
        }
    }

    public void OnConrollerMove(Vector2 _lookAt)
    {
        if (m_state != null)
            SetState(CharacterStateType.None);

        move.OnMoveUpdate(_lookAt.normalized * m_data.moveSpeed);
    }

    public void SetState(CharacterStateType _stateType)
    {
        m_state?.Stop();
        m_state = m_dbState.ContainsKey(_stateType) ? m_dbState[_stateType] : null;
        m_state?.Start();

        m_stateType = _stateType;
    }

    public bool OnDamage(int _damage)
    {
        m_data.health -= _damage;
        if (m_data.health <= 0)
        {
            m_data.health = 0;
            m_state?.Stop();
            anim.Play(CharacterAnimType.Die_1 + UnityEngine.Random.Range(0, 2));

            transform.GetComponent<Collider2D>("Character").enabled = false;
        }

        Signal.instance.UpdateHP.Emit(this);

        return m_data.health == 0;
    }

    public void Respawn(bool _isSetState = true)
    {
        if (_isSetState)
            SetState(TeamManager.instance.teamState);

        m_data.health = m_data.healthMax;
        transform.GetComponent<Collider2D>("Character").enabled = true;
    }

    public void DeleteElement()
    {
        Destroy(m_element.effect_renderer.gameObject);
        m_element.effect_canvas.parent.gameObject.SetActive(false);
    }

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;
    public ElementData element => m_element;
    [Serializable]
    public struct ElementData
    {
        public bool isMain;

        public Transform panel;
        public Rigidbody2D rig;
        public Animator animator;

        public Transform effect_canvas;
        public Transform effect_renderer;

        public Transform cameraPos;

        public CharacterAnimationClipData animationClipData;

        public void Initialize(Transform _transform)
        {
            rig = _transform.GetComponent<Rigidbody2D>();
            panel = _transform.Find("Character/Panel");
            animator = _transform.GetComponent<Animator>("Character/Panel/Parts");
            effect_canvas = _transform.Find("Character/Canvas/Effect");
            effect_renderer = _transform.Find("Character/Effect_Renderer");
            cameraPos = panel.Find("CameraPos");
        }
    }
}