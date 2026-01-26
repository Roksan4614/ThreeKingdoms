using UnityEditor.ShaderGraph.Internal;
using UnityEngine;


public class Character_Woker_Anim : Character_Worker
{
    Animator m_animator;

    public CharacterAnimType playingAnimType { get; private set; }

    public Character_Woker_Anim(CharacterComponent _owner, CharacterAnimationClipData _clipData) : base(_owner)
    {
        m_animator = m_owner.rig.transform.GetComponent<Animator>("Character/Panel/Parts");

        var overrideAnimator = new AnimatorOverrideController(m_animator.runtimeAnimatorController);

        for (var i = CharacterAnimType.NONE + 1; i < CharacterAnimType.MAX; i++)
        {
            string key = $"Character_{i}_NONE";

            var prevAc = overrideAnimator[key];
            if (prevAc == null)
                continue;

            overrideAnimator[key] = _clipData.GetClip(i) ?? prevAc;
        }

        m_animator.runtimeAnimatorController = overrideAnimator;

    }

    public void PlayAnimation(CharacterAnimType _animType)
    {
        playingAnimType = _animType;
        PlayAnimation(_animType, 0);
    }

    public void PlayAnimation(CharacterAnimType _animType, int _layerIndex)
    {
        m_animator.Play(playingAnimType.ToString(), _layerIndex, 0);
        m_animator.Update(0);
    }
}
