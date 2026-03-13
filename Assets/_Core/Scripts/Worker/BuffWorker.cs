using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public enum BuffType
{
    NONE = -1,

    BUFF_NO_TAKEN_DAMAGE,

    DEBUFF_NO_SKILL,

    MAX
}

public struct BuffData
{
    public long hash;
    public long endTick;
}

public static class BuffWorker
{
    public static long AddBuff(CharacterComponent _hero, BuffType buffType)
     => _hero.buff.Add(buffType).hash;


    public static async UniTask AddBuffAsync(CharacterComponent _hero, BuffType buffType)
    {

    }
}
