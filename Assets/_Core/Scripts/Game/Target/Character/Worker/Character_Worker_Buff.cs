using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Character_Worker_Buff : Character_Worker
{
    public Character_Worker_Buff(CharacterComponent _owner) : base(_owner) { }

    async UniTask Timer(long _hash, BuffType _buffType, float _duration)
    {
        await UniTask.WaitForSeconds(_duration);
        //Remove(_hash, _buffType);
        Remove(_hash);
    }

    public long Add(BuffType _buffType, float _value = 0, float _duration = 0)
    {
        BuffData buffData = new();
        buffData.hash = Utils.GetUTC().Ticks;
        buffData.value = _value;

        if (m_dbBuff.ContainsKey(_buffType))
            m_dbBuff[_buffType].Add(buffData);
        else
            m_dbBuff.Add(_buffType, new() { buffData });

        if (_duration > 0)
            Timer(buffData.hash, _buffType, _duration).Forget();

        return buffData.hash;
    }

    public void Remove(long _hash, BuffType _buffType = BuffType.NONE)
    {
        bool isContainsKey = m_dbBuff.ContainsKey(_buffType);
        if (_buffType > BuffType.NONE)
        {
            if (_hash > 0)
            {
                if (isContainsKey)
                {
                    int idx = m_dbBuff[_buffType].FindIndex(x => x.hash == _hash);
                    m_dbBuff[_buffType].RemoveAt(idx);
                }
            }
            else
            {
                m_dbBuff.Remove(_buffType);
                isContainsKey = false;
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
        if (m_dbBuff.ContainsKey(_buffType) && m_dbBuff[_buffType].Count > 0)
            return true;

        return false;
    }

    Dictionary<BuffType, List<BuffData>> m_dbBuff = new();
}
