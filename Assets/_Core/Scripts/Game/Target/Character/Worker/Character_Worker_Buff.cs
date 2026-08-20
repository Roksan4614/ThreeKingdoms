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
        Remove(_buffType, _hash);
    }

    public long Add(BuffType _buffType, float _value = 0, float _duration = 0)
    {
        BuffData buffData = new();
        buffData.hash = Utils.GetUTC().Ticks;
        buffData.value = _value;

        if (m_dbBuff.ContainsKey(_buffType))
            m_dbBuff[_buffType].Add(buffData);
        else
        {
            m_dbBuff.Add(_buffType, new() { buffData });
            //m_owner.buffs.Add(_buffType);
        }

        if (_duration > 0)
            Timer(buffData.hash, _buffType, _duration).Forget();

        //if (m_owner.isMain)
        //    IngameLog.Add("BUFF ADD: " + _buffType + ": " + buffData.hash);

        return buffData.hash;
    }

    public void RemoveAll()
        => Remove(BuffType.NONE, -1);

    public void Remove(long _hash, BuffType _buffType = BuffType.NONE)
    {
        if (_buffType > BuffType.NONE)
            Remove(_buffType, _hash);
        else
        {
            foreach (var b in m_dbBuff)
            {
                for (int i = 0; i < b.Value.Count; i++)
                {
                    if (b.Value[i].hash == _hash)
                    {
                        Remove(b.Key, _hash);
                        return;
                    }
                }
            }
        }
    }

    public void Remove(BuffType _buffType, long _hash = -1)
    {
        if (_buffType > BuffType.NONE)
        {
            bool isContainsKey = m_dbBuff.ContainsKey(_buffType);

            if (_hash > 0)
            {
                if (isContainsKey)
                {
                    var buff = m_dbBuff[_buffType];
                    for (int i = 0; i < buff.Count; i++)
                    {
                        if (buff[i].hash == _hash)
                        {
                            //if (m_owner.isMain)
                            //    IngameLog.Add("BUFF REMOVE: " + _buffType + ": " + _hash);
                            m_dbBuff[_buffType].RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            else
            {
                if (m_owner.isMain)
                    IngameLog.Add("BUFF REMOVE: " + _buffType);
                m_dbBuff.Remove(_buffType);
                //m_owner.buffs.Remove(_buffType);
                isContainsKey = false;
            }

            if (isContainsKey && m_dbBuff[_buffType].Count == 0)
                m_dbBuff.Remove(_buffType);
        }
        else
        {
            if (m_owner.isMain)
                IngameLog.Add("BUFF REMOVE ALL");
            m_dbBuff.Clear();
        }
    }

    public bool IsActive(BuffType _buffType)
    {
        if (m_dbBuff.ContainsKey(_buffType) && m_dbBuff[_buffType].Count > 0)
            return true;

        return false;
    }

    Dictionary<BuffType, List<BuffData>> m_dbBuff = new();
}
