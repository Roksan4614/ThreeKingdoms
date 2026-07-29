using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class TimeManager
{
    public static TimeManager instance { get; private set; } = new();

    CancellationTokenSource m_cts;
    public async UniTask InitializeAsync()
    {
        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;

        var lastDate = Utils.GetUTC().Date;

        while (lastDate == Utils.GetUTC().Date)
            await UniTask.NextFrame(token);

        Signal.instance.DayChange.Emit();
        InitializeAsync().Forget();
    }

    public void Release()
    {
        instance = null;
        m_cts = m_cts.ReleaseCTS();
    }
}
