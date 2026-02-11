using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;


public class Character_Woker_Anim : Character_Worker
{
    Animator m_animator;

    public CharacterAnimType animType { get; private set; }

    public Character_Woker_Anim(CharacterComponent _owner) : base(_owner)
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

    public void Play(CharacterAnimType _animType)
     {
        animType = _animType;
        Play(_animType, 0);
    }

    public void Play(CharacterAnimType _animType, int _layerIndex)
    {
        m_animator.Play(animType.ToString(), _layerIndex, 0);
        m_animator.Update(0);
    }

    public void AttackMotionStart()
    {
        m_animator.speed = 0;
        Play(CharacterAnimType.Attack);
    }

    public void AttackMotionEnd() => m_animator.speed = 1;
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
