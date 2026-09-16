using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

public class ActionRotation : MonoBehaviour
{
    private void OnEnable()
        => StartAsync().Forget();

    private void OnDisable()
        => m_cts = m_cts.ReleaseCTS();

    CancellationTokenSource m_cts;
    async UniTask StartAsync()
    {
        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;

        transform.rotation = Quaternion.Euler(Vector3.zero);
        await transform.DORotate(new Vector3(0f, 0f, 360f), 20f, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear).ToUniTask(cancellationToken: token);
    }
}
