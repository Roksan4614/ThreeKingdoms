using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ThreeKingdoms.Client.Server
{
    // A raid hit is removed only after a receipt or an explicit server rejection, never on a transport error.
    public sealed class RaidDamageOutbox
    {
        readonly Queue<JObject> pending = new Queue<JObject>();
        readonly Action<IReadOnlyList<JObject>> save;
        bool sending;
        public int Count => pending.Count;

        public static bool IsDefinitiveRejection(string code) => code == "RAID_PHASE_CHANGED"
            || code == "RAID_ROUND_NOT_FOUND" || code == "RAID_NOT_JOINED"
            || code == "INVALID_REQUEST" || code == "REQUEST_ID_REUSED";

        public RaidDamageOutbox(IEnumerable<JObject> restored, Action<IReadOnlyList<JObject>> persist)
        {
            save = persist ?? throw new ArgumentNullException(nameof(persist));
            var ids = new HashSet<string>();
            foreach (var request in restored ?? Array.Empty<JObject>())
            {
                Validate(request);
                if (!ids.Add((string)request["request_id"])) throw new InvalidOperationException("RAID_DAMAGE_CACHE_DUPLICATE_ID");
                pending.Enqueue((JObject)request.DeepClone());
            }
        }

        public void Enqueue(JObject request)
        {
            Validate(request);
            if (pending.Any(item => (string)item["request_id"] == (string)request["request_id"]))
                throw new InvalidOperationException("RAID_DAMAGE_DUPLICATE_ID");
            pending.Enqueue((JObject)request.DeepClone());
            Save();
        }

        public async Task DrainAsync<T>(Func<JObject, CancellationToken, Task<T>> send,
            Func<Exception, bool> definitiveRejection, Action<JObject, T> accepted,
            Action<JObject, Exception, bool> failed, Func<CancellationToken, Task> retryDelay, CancellationToken token)
        {
            if (sending) return;
            sending = true;
            try
            {
                while (pending.Count > 0)
                {
                    token.ThrowIfCancellationRequested();
                    var request = pending.Peek();
                    T result;
                    try
                    {
                        result = await send((JObject)request.DeepClone(), token);
                        token.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                    catch (Exception error)
                    {
                        var rejected = definitiveRejection(error);
                        if (rejected) { pending.Dequeue(); Save(); }
                        failed?.Invoke((JObject)request.DeepClone(), error, rejected);
                        if (!rejected) await retryDelay(token);
                        continue;
                    }
                    pending.Dequeue();
                    Save();
                    accepted?.Invoke((JObject)request.DeepClone(), result);
                }
            }
            finally { sending = false; }
        }

        void Save() => save(pending.Select(request => (JObject)request.DeepClone()).ToArray());

        static void Validate(JObject request)
        {
            if (request == null || (string)request["type"] != "RAID_DAMAGE"
                || !Guid.TryParse((string)request["request_id"], out _)
                || string.IsNullOrEmpty((string)request["raid_id"])
                || ((string)request["phase"] != "normal" && (string)request["phase"] != "jin")
                || !long.TryParse((string)request["damage"], out var damage) || damage <= 0)
                throw new InvalidOperationException("RAID_DAMAGE_CACHE_INVALID");
        }
    }
}
