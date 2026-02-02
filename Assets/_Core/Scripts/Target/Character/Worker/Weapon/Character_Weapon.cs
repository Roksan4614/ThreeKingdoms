using System;
using System.Collections;
using UnityEngine;

public class Character_Weapon : MonoBehaviour
{
    public int t;

    protected bool m_isUseSkill;
    public virtual IEnumerator DoAttack(CharacterComponent _owner) { yield return null; }

    public virtual void UseSkill() { }

    public virtual void EventAttackHit(CharacterComponent _owner)
    {
        var target = _owner.target.target;
        if (target == null || target.isLive == false)
            return;

        EffectWorker.instance.SlotDamageTakenEffect(new() { attacker = _owner.transform, target = target.transform, });

        if (target.OnDamage(_owner.data.attackPower))
            _owner.target.SetTarget(null);
    }
}
