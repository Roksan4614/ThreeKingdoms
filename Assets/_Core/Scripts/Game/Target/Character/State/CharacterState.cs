using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using UnityEngine;

public enum CharacterStateType
{
    None = -1,

    Wait, //대기
    SearchEnemy, // 적에게 접근
    Battle, // 전투중

    Following, // 주장 따라가기

    Max,
}

public class CharacterState
{
    public CharacterStateType stateType { get; protected set; }

    protected CharacterComponent m_owner;
    protected CharacterState(CharacterStateType _stateType, CharacterComponent _owner)
    {
        stateType = _stateType;
        m_owner = _owner;
    }

    protected CancellationTokenSource m_cts;

    public virtual void Start(params object[] _data)
    {
        m_cts = m_cts.ReleaseCTS(true);
        UpdateAsync().Forget();
    }

    public virtual void Stop()
        => m_cts = m_cts.ReleaseCTS();

    public virtual async UniTask UpdateAsync()
        => await UniTask.Yield();

}
