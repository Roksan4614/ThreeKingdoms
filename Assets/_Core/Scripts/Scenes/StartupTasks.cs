using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;

// Startup may launch siblings before one fails. Drain every sibling before allowing a retry.
public static class StartupTasks
{
    public static async UniTask WaitAllAsync(IEnumerable<UniTask> tasks, Exception initialFailure = null)
    {
        var failures = new List<Exception>();
        if (initialFailure != null) failures.Add(initialFailure);
        async UniTask Observe(UniTask task)
        {
            try { await task; }
            catch (Exception error) { failures.Add(error); }
        }
        await UniTask.WhenAll(tasks.Select(Observe));
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException("Game initialization failed", failures);
    }
}
