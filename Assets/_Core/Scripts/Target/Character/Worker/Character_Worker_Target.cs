using System.Collections.Generic;
using UnityEngine;

public class Character_Worker_Target : Character_Worker
{
    public Character_Worker_Target(CharacterComponent _owner) : base(_owner)
    {
    }

    public CharacterComponent target { get; private set; }

    List<CharacterComponent> m_targetList = new();
    //public IReadOnlyList<CharacterComponent> targetList => m_targetList;

    public void SetTarget(CharacterComponent _target)
        => target = _target;

    public void AddTarget(CharacterComponent _target)
    {
        if (IsEnemy(_target) == false || m_targetList.Contains(_target) == true)
            return;

        m_targetList.Add(_target);
    }

    public void RemoveTarget(CharacterComponent _target)
    {
        if (IsEnemy(_target) == false)
            return;

        m_targetList.Remove(_target);
    }

    bool IsEnemy(CharacterComponent _target)
    {
        return _target != null &&
            _target.factionType != m_owner.factionType &&
            _target.factionType < FactionType.ETC;
    }

    public bool Contains(CharacterComponent _target)
    {
        for( int i = 0; i < m_targetList.Count; i++)
        {
            if (m_targetList[i] == _target)
                return true;
        }
        return false;
    }

    public CharacterComponent nearestEnemy
    {
        get
        {
            if (m_targetList.Count == 0)
                return null;

            CharacterComponent result = null;
            float minDist = float.MaxValue;
            var posOwner = m_owner.transform.position;

            for (int i = 0; i < m_targetList.Count; i++)
            {
                var enemy = m_targetList[i];

                if (enemy.isLive == false)
                    continue;

                float sqrDist = (enemy.transform.position - posOwner).sqrMagnitude;
                if (sqrDist < minDist)
                {
                    minDist = sqrDist;
                    result = enemy;
                }
            }

            return result;
        }
    }
}
