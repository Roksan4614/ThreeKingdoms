using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


public class CharacterComponent : MonoBehaviour
{
    [SerializeField]
    Data_Character m_data;
    [SerializeField]
    bool m_isMain;
    [SerializeField]
    FactionType m_faction;

    [SerializeField]
    CharacterAnimationClipData m_animationClipData;

    public Transform panel { get; private set; }
    public Rigidbody2D rig { get; private set; }

    public Character_Woker_Anim anim { get; private set; }
    public Character_Woker_Move move { get; private set; }
    public Character_Worker_Attack attack { get; private set; }

    Dictionary<CharacterStateType, CharacterState_> m_dbState = new();
    CharacterState_ m_state;

    SortingGroup m_sortingGroup;

    public Data_Character data => m_data;
    public bool isLive => data.health > 0;
    public bool isMain => m_isMain;
    public FactionType factionType => m_faction;
    public TeamPositionType teamPosition { get; private set; }

    public void SetMain(bool _isMain) => m_isMain = _isMain;

    private void Awake()
    {
        rig = transform.GetComponent<Rigidbody2D>();

        m_sortingGroup = transform.GetComponent<SortingGroup>();
        panel = rig.transform.Find("Character/Panel");

        anim = new(this, m_animationClipData); m_animationClipData = default;
        move = new(this);
        attack = new(this);

        m_dbState.Add(CharacterStateType.Wait, new CharacterState_Wait(this));
        m_dbState.Add(CharacterStateType.Battle, new CharacterState_Battle(this));

        SetState(CharacterStateType.Wait);

        m_data.SetDefault();
        m_sortingGroup.sortingOrder = 0;
    }

    float m_prevPosY;
    public void UpdateSortingOreder()
    {
        if (m_prevPosY != transform.position.y)
        {
            m_prevPosY = transform.position.y;
            m_sortingGroup.sortingOrder = (int)(transform.position.y * -10f);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.PlayAnimation(CharacterAnimType.Attack);
        }
    }

    private void LateUpdate()
    {
        m_state?.LateUpdate();
        UpdateSortingOreder();
    }

    public void SetFaction(FactionType _factionType) => m_faction = _factionType;

    public void SetTeamPosition(TeamPositionType _teamPosition, Vector3 _position)
    {
        teamPosition = _teamPosition;
        anim.PlayAnimation(CharacterAnimType.Idle);
        transform.position = _position;
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
        {
            var character = _collision.transform.parent.GetComponent<CharacterComponent>();

            bool isEnemy = m_faction != character.factionType;
            if (isEnemy)
            {
            }
        }
    }

    public void OnConrollerMove(Vector2 _lookAt)
    {
        if (m_state != null)
            SetState(CharacterStateType.None);
        move.OnMoveUpdate(_lookAt);
    }

    public void SetState(CharacterStateType _stateType)
    {
        m_state?.Stop();
        m_state = m_dbState.ContainsKey(_stateType) ? m_dbState[_stateType] : null;
        m_state?.Start();
    }
}

[Serializable]
public struct CharacterAnimationClipData
{
    public AnimationClip attack;

    public AnimationClip GetClip(CharacterAnimType _animType) => _animType switch
    {
        CharacterAnimType.Attack => attack,
        _ => null,
    };
}