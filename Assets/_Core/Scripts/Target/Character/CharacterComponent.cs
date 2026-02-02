using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


public class CharacterComponent : TargetComponent
{
    [SerializeField]
    Data_Character m_data;
    [SerializeField]
    bool m_isMain;
    [SerializeField]
    FactionType m_faction;
    [SerializeField]
    CharacterAnimationClipData m_animationClipData;

    CharacterState m_state;

    Dictionary<CharacterStateType, CharacterState> m_dbState = new();

    public Character_Woker_Anim anim { get; private set; }
    public Character_Woker_Move move { get; private set; }
    public Character_Worker_Attack attack { get; private set; }
    public Character_Worker_Target target { get; private set; }
    public Transform panel { get; private set; }
    public Rigidbody2D rig { get; private set; }
    public Data_Character data => m_data;
    public override bool isLive => data.health > 0;
    public bool isMain => m_isMain;
    public FactionType factionType => m_faction;
    public TeamPositionType teamPosition { get; private set; }

    public void SetMain(bool _isMain) => m_isMain = _isMain;

    protected override void Awake()
    {
        base.Awake();
        rig = transform.GetComponent<Rigidbody2D>();

        panel = rig.transform.Find("Character/Panel");

        anim = new(this, m_animationClipData); m_animationClipData = default;
        move = new(this);
        attack = new(this);
        target = new(this);

        m_dbState.Add(CharacterStateType.Wait, new CharacterState_Wait(this));
        m_dbState.Add(CharacterStateType.SearchEnemy, new CharacterState_SearchEnemy(this));
        m_dbState.Add(CharacterStateType.Battle, new CharacterState_Battle(this));

        SetState(CharacterStateType.Wait);

        // TEST
        m_data.SetDefault();

        if (m_faction == FactionType.Enemy)
        {
            m_data.attackPower /= 2;
            m_data.healthMax = m_data.health /= 2;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.Play(CharacterAnimType.Attack);

            if(isMain)
            {
                EffectWorker.instance.SlotDamageTakenEffect(new() { attacker = transform, target = StageManager.instance.GetNearestEnemy(transform.position).transform });
            }
        }
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
            return true;
        }
        return false;
    }
}