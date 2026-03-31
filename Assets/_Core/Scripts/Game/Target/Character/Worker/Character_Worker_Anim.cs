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
            string key = $"Character_{i}_NONE";

            var prevAc = overrideAnimator[key];
            if (prevAc == null)
                continue;

            overrideAnimator[key] = m_owner.element.animationClipData.GetClip(i) ?? prevAc;
        }

        m_animator.runtimeAnimatorController = overrideAnimator;
    }

    public bool IsType(CharacterAnimType _animType, int _layerIndex = 0)
        => m_animator.GetCurrentAnimatorStateInfo(_layerIndex).IsName(_animType.ToString());


    public void Play(CharacterAnimType _animType)
    {
        Play(_animType, 0);
    }

    public void Play(CharacterAnimType _animType, int _layerIndex)
    {
        if (_animType == CharacterAnimType.Attack || _animType == CharacterAnimType.Skill)
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

}

[Serializable]
public struct CharacterAnimationClipData
{
    public AnimationClip attack;
    public AnimationClip skill;

    public AnimationClip GetClip(CharacterAnimType _animType) => _animType switch
    {
        CharacterAnimType.Attack => attack,
        CharacterAnimType.Skill => skill,
        _ => null,
    };
}
