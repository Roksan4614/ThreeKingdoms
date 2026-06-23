using System;
using UnityEngine;


public class Character_Worker_Anim : Character_Worker
{
    Animator m_animator;

    public Character_Worker_Anim(CharacterComponent _owner) : base(_owner)
    {
        m_animator = m_owner.element.animator;

        var overrideAnimator = new AnimatorOverrideController(m_animator.runtimeAnimatorController);

        for (var i = CharacterAnimType.NONE + 1; i < CharacterAnimType.MAX; i++)
        {
            var clip = m_owner.element.animationClipData.GetClip(i);

            // Attack move 가 없을 땐 일단 기본으로 채워주자
            if (i == CharacterAnimType.Attack_Move && clip == null)
                clip = m_owner.element.animationClipData.GetClip(CharacterAnimType.Attack);

            if (clip == null)
                continue;

            string key = $"Character_{i}";
            var prevAc = overrideAnimator[key];
            if (prevAc == null)
                continue;

            overrideAnimator[key] = clip;
        }

        m_animator.runtimeAnimatorController = overrideAnimator;
    }

    public bool IsType(CharacterAnimType _animType, int _layerIndex = 0)
        => GetStateInfo(_layerIndex).IsName(_animType.ToString());

    public void Play(string _anim, int _layerIndex = 0)
        => m_animator.CrossFade(_anim, 0, _layerIndex, 0);

    public void Play(CharacterAnimType _animType)
    {
        Play(_animType, 0);
    }

    public void Play(CharacterAnimType _animType, int _layerIndex)
    {
        if (_animType == CharacterAnimType.Attack ||
            _animType == CharacterAnimType.Attack_Move ||
            _animType == CharacterAnimType.Skill)
            m_owner.attack.isRunningAttack = true;

        //m_animator.Play(_animType.ToString(), _layerIndex, 0);
        m_animator.CrossFade(_animType.ToString(), 0, _layerIndex, 0);

        //if (m_owner.isMain == true)
        //    IngameLog.Add($"[ANIM] PLAY: {_animType}{(_layerIndex == 0 ? "" : $"/{_layerIndex}")}");
    }

    public void PlayAttack(bool _isShowFx = false, bool _isShake = false)
    {
        Play(CharacterAnimType.Attack);
        if (_isShowFx)
            m_owner.attack.ShowSlashEffect(_isShake);
    }

    public void AttackMotionFirstFrame(CharacterAnimType _animType = CharacterAnimType.Attack, int _layerIndex = 0)
    {
        m_animator.speed = 0;
        Play(_animType, _layerIndex);
    }

    public void AttackMotionEnd() => m_animator.speed = 1;

    public AnimatorStateInfo GetStateInfo(int _layerIndex = 0)
        => m_animator.GetCurrentAnimatorStateInfo(_layerIndex);
}

[Serializable]
public struct CharacterAnimationClipData
{
    [SerializeField] AnimationClip idle;
    [SerializeField] AnimationClip attack;
    [SerializeField] AnimationClip attack_move;
    [SerializeField] AnimationClip skill;
    [SerializeField] AnimationClip dash;
    [SerializeField] AnimationClip dash_back;
    [SerializeField] AnimationClip die1;
    [SerializeField] AnimationClip die2;

    public AnimationClip GetClip(CharacterAnimType _animType) => _animType switch
    {
        CharacterAnimType.Idle => idle,
        CharacterAnimType.Attack => attack,
        CharacterAnimType.Attack_Move => attack_move,
        CharacterAnimType.Skill => skill,
        CharacterAnimType.Die_1 => die1,
        CharacterAnimType.Die_2 => die2,
        CharacterAnimType.Dash => dash,
        CharacterAnimType.Dash_Back => dash_back,
        _ => null,
    };
}
