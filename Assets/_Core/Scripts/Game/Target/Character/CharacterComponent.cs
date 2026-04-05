using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


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

    public Character_Worker_Buff buff { get; private set; }
    public Character_Worker_Anim anim { get; private set; }
    public Character_Worker_Move move { get; private set; }
    public Character_Worker_Attack attack { get; private set; }
    public Character_Worker_Target target { get; private set; }
    public Character_Worker_Talkbox talkbox { get; private set; }
    public Transform panel => m_element.panel;
    public Rigidbody2D rig => m_element.rig;
    public TableHeroData data => m_data;
    public HeroInfoData info => m_info;
    public override bool isLive => gameObject != null && data.health > 0;
    public bool isMain => m_info.isMain;
    public FactionType factionType => m_faction;
    public TeamPositionType teamPosition { get; private set; } = TeamPositionType.NONE;

    protected virtual void Awake()
    {
        anim = new(this);// m_element.animator = default;
        move = new(this);
        attack = new(this);
        target = new(this);
        talkbox = new(this);
        buff = new(this);

        m_dbState.Add(CharacterStateType.Wait, new CharacterState_Wait(this));
        m_dbState.Add(CharacterStateType.SearchEnemy, new CharacterState_SearchEnemy(this));
        m_dbState.Add(CharacterStateType.Battle, new CharacterState_Battle(this));

        SetState(CharacterStateType.None);

        if (m_faction == FactionType.Enemy)
        {
            m_data.attackPower /= 2;
            m_data.healthMax = m_data.health /= 2;
        }
    }

    private void Update()
    {
        if (isMain == false)
            return;
    }

    public virtual void SetHeroData(string _key)
    {
        m_info = DataManager.userInfo.GetHeroInfoData(_key);
        m_data = TableManager.hero.GetHeroData(_key, m_info.grade, m_info.enchantLevel);

        attack.ResetFX();
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

    public void OnConrollerMove(Vector2 _lookAt, bool _isAnim = true)
    {
        if (m_state != null)
            SetState(CharacterStateType.None);

        move.OnMoveUpdate(_lookAt.normalized * m_data.moveSpeed, _isAnim);
    }

    public void SetState(CharacterStateType _stateType)
    {
        m_state?.Stop();
        m_state = m_dbState.ContainsKey(_stateType) ? m_dbState[_stateType] : null;
        m_state?.Start();

        m_stateType = _stateType;
    }

    public virtual bool OnDamage(CharacterComponent _attacker, int _damage)
    {
        if (_attacker != null &&
            target.target == null &&
            isLive == true &&
            ControllerManager.instance.IsControll(this) == false)
        {
            move.MoveTarget(_attacker, true);
        }

        if (buff.IsActive(BuffType.BUFF_NO_TAKEN_DAMAGE) == false)
        {
            m_data.health -= _damage;
            if (m_data.health <= 0)
            {
                m_data.health = 0;
                m_state?.Stop();
                anim.Play(CharacterAnimType.Die_1 + UnityEngine.Random.Range(0, 2));

                m_element.collider.enabled = false;
                StopAllCoroutines();
                target.RemoveAll();
            }

            Signal.instance.UpdateHP.Emit(this);
        }

        return m_data.health == 0;
    }

    public void Respawn(bool _isSetState = true)
    {
        if (_isSetState)
            SetState(TeamManager.instance.teamState);

        target.RemoveAll();
        move.MoveStop();

        m_data.health = m_data.healthMax;
        m_element.collider.enabled = true;

        SetColorParts(Color.white);
    }

    public void DeleteElement()
    {
        Destroy(m_element.effect_renderer.gameObject);
        m_element.effect_canvas.parent.gameObject.SetActive(false);
    }

    public void DestroyCharacter()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }

    public void SetColorParts(Color _color, float _duration = 0)
    {
        if (_color == Color.white)
        {
            if (m_element.partsRenders[0].color == m_element.colorParts[0])
                return;

            if (_duration == 0)
                for (int i = 0; i < m_element.partsRenders.Length; i++)
                    m_element.partsRenders[i].color = m_element.colorParts[i];
            else
                for (int i = 0; i < m_element.partsRenders.Length; i++)
                    m_element.partsRenders[i].DOColor(m_element.colorParts[i], _duration);
        }
        else
        {
            if (m_element.partsRenders[0].color == _color)
                return;

            if (_duration == 0)
            {
                for (int i = 0; i < m_element.partsRenders.Length; i++)
                    m_element.partsRenders[i].color = _color;
            }
            else
            {
                for (int i = 0; i < m_element.partsRenders.Length; i++)
                    m_element.partsRenders[i].DOColor(_color, _duration);
            }
        }
    }

    public override void OnManualValidate()
    {
        m_element.Initialize(transform);

        base.OnManualValidate();
    }

    //[SerializeField, HideInInspector]
    [SerializeField]
    ElementData m_element;
    public ElementData element => m_element;
    [Serializable]
    public struct ElementData
    {
        [SerializeField] Transform m_panel;
        [SerializeField] Rigidbody2D m_rig;
        [SerializeField] Animator m_animator;
        [SerializeField] Transform m_effect_canvas;
        [SerializeField] Transform m_effect_renderer;
        [SerializeField] Transform m_cameraPos;
        [SerializeField] Transform m_skillRage;
        [SerializeField] Collider2D m_collider;
        [SerializeField] TextMeshProUGUI m_txtTalk;
        [SerializeField] CharacterAnimationClipData m_animationClipData;

        public SpriteRenderer[] partsRenders;
        public Color[] colorParts;

        public Transform panel => m_panel;
        public Rigidbody2D rig => m_rig;
        public Animator animator => m_animator;
        public Transform effect_canvas => m_effect_canvas;
        public Transform effect_renderer => m_effect_renderer;
        public Transform cameraPos => m_cameraPos;
        public Transform skillRange => m_skillRage;
        public Collider2D collider => m_collider;
        public CharacterAnimationClipData animationClipData => m_animationClipData;
        public TextMeshProUGUI txtTalk => m_txtTalk;

        public Transform parts => m_animator.transform;

        public void Initialize(Transform _transform)
        {
            m_rig = _transform.GetComponent<Rigidbody2D>();
            m_panel = _transform.Find("Character/Panel");
            m_animator = _transform.GetComponent<Animator>("Character/Panel/Parts");
            m_effect_canvas = _transform.Find("Character/Canvas/Effect");
            m_effect_renderer = _transform.Find("Character/Effect_Renderer");
            m_cameraPos = panel.Find("CameraPos");
            m_skillRage = _transform.Find("SkillRange");
            m_txtTalk = _transform.GetComponent<TextMeshProUGUI>("Character/Canvas/Talkbox/txt_talk");
            m_collider = panel.parent.GetComponent<Collider2D>();

            partsRenders = m_animator.transform.GetComponentsInChildren<SpriteRenderer>(true);
            colorParts = partsRenders.Select(x => x.color).ToArray();
        }
    }
}