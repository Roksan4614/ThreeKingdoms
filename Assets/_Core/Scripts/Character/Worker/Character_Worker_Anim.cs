using UnityEditor.ShaderGraph.Internal;
using UnityEngine;


public class Character_Woker_Anim: Character_Worker
{
    Animator m_animator;

    public CharacterAnimType playingAnimType { get; private set; }

    public Character_Woker_Anim(CharacterComponent _owner) : base(_owner)
    {
        m_animator = m_owner.rig.transform.GetComponent<Animator>("Character/Panel/Parts");
    }

    public void PlayAnimation(CharacterAnimType _animType)
    {
        playingAnimType = _animType;
        m_animator.Play(playingAnimType.ToString());
    }

    public void PlayAnimation(CharacterAnimType _animType, int _layerIndex)
    {
        m_animator.Play(playingAnimType.ToString(), _layerIndex);
    }
}
