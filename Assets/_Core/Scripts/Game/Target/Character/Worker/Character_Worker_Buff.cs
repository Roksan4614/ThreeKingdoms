using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Character_Worker_Buff : Character_Worker
{
    public Character_Worker_Buff(CharacterComponent _owner) : base(_owner) { }

    public BuffData Add(BuffType _buffType, float _duration = -1)
    {
        BuffData buffData = new();
        buffData.hash = Utils.GetUTCTicks();
        if (_duration > 0)
            buffData.endTick = Utils.GetUTCTicks(_duration);

        if (m_dbBuff.ContainsKey(_buffType))
            m_dbBuff[_buffType].Add(buffData);
        else
            m_dbBuff.Add(_buffType, new() { buffData });

        return buffData;
    }

    public void Remove(long _hash, BuffType _buffType = BuffType.NONE)
    {
        bool isContainsKey = m_dbBuff.ContainsKey(_buffType);
        if (_buffType > BuffType.NONE)
        {
            if (isContainsKey)
            {
                int idx = m_dbBuff[_buffType].FindIndex(x => x.hash == _hash);
                m_dbBuff[_buffType].RemoveAt(idx);
            }
        }
        else
        {
            foreach (var d in m_dbBuff)
            {
                int idx = d.Value.FindIndex(x => x.hash == _hash);
                if (idx > -1)
                {
                    d.Value.RemoveAt(idx);
                    _buffType = d.Key;
                    isContainsKey = true;
                    break;
                }
            }
        }

        if (isContainsKey && m_dbBuff[_buffType].Count == 0)
            m_dbBuff.Remove(_buffType);
    }

    public void RemoveAll(BuffType _buffType = BuffType.NONE)
    {
        if (_buffType == BuffType.NONE)
            m_dbBuff.Clear();
        else if (m_dbBuff.ContainsKey(_buffType))
            m_dbBuff.Remove(_buffType);
    }

    public bool IsActive(BuffType _buffType)
    {
        var nowTick = Utils.GetUTCTicks();

        if (m_dbBuff.ContainsKey(_buffType))
        {
            m_dbBuff[_buffType] = m_dbBuff[_buffType].Where(x => x.endTick == 0 || x.endTick > nowTick).ToList();

            if (m_dbBuff[_buffType].Count > 0)
                return true;
        }

        return false;
    }

    Dictionary<BuffType, List<BuffData>> m_dbBuff = new();
}
