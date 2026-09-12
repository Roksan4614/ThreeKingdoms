using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
#if UNITY_WEBGL && !UNITY_EDITOR
using ClientWebSocket = ThreeKingdoms.Client.Server.WebGlClientWebSocket;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThreeKingdoms.Client.Server
{
    public sealed class RaidSocketClient : IDisposable
    {
        private ClientWebSocket socket;
        private CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly SemaphoreSlim connectionGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim sendGate = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, PendingRequest> requests = new ConcurrentDictionary<string, PendingRequest>();
        private readonly ConcurrentQueue<JObject> broadcasts = new ConcurrentQueue<JObject>();
        private bool authenticated;
        private readonly long accountUid = GameServer.Uid;
        private int generation;
        private int disposed;
        private sealed class PendingRequest
        {
            public readonly ClientWebSocket Socket;
            public readonly TaskCompletionSource<JObject> Completion = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            public PendingRequest(ClientWebSocket socket) { Socket = socket; }
        }
        public bool IsDisposed => Volatile.Read(ref disposed) != 0;
        public int Generation => Volatile.Read(ref generation);
        public bool IsConnected => socket?.State == WebSocketState.Open && authenticated;
        public bool TryReadBroadcast(out JObject message) => broadcasts.TryDequeue(out message);

        public async Task ConnectAsync(CancellationToken token) { await GetConnectionAsync(token); }

        private async Task<ClientWebSocket> GetConnectionAsync(CancellationToken token)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(RaidSocketClient));
            var lifetimeToken = lifetime.Token;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetimeToken);
            token = linked.Token;
            await connectionGate.WaitAsync(token);
            ClientWebSocket active = null;
            try
            {
                if (IsConnected) return socket;
                authenticated = false;
                socket?.Dispose();
                active = new ClientWebSocket();
                socket = active;
                var activeAuthentication = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
                await active.ConnectAsync(new Uri(GameServer.Settings.RaidUrl), token);
                _ = ReceiveAsync(active, activeAuthentication, lifetimeToken);
                if (GameServer.Uid != accountUid) throw new OperationCanceledException("RAID_ACCOUNT_CHANGED", token);
                await SendAsync(active, new JObject { ["type"] = "AUTH", ["uid"] = accountUid, ["session_key"] = GameServer.SessionKey }, token);
                var auth = await WaitAsync(activeAuthentication.Task, token);
                if ((string)auth["type"] != "AUTH_OK") throw new GameServerException((string)auth["reason"] ?? "RAID_AUTH_FAILED", 0);
                if (active.State != WebSocketState.Open) throw new IOException("RAID_SOCKET_CLOSED_DURING_AUTH");
                authenticated = true;
                Interlocked.Increment(ref generation);
                return active;
            }
            catch { AbortTransport(active); throw; }
            finally { connectionGate.Release(); }
        }

        public async Task<T> RequestAsync<T>(JObject request, CancellationToken token)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(RaidSocketClient));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
            token = linked.Token;
            var id = (string)request["request_id"];
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Raid request_id is required");
            // Reconnect retries preserve the exact UUID and payload, including the original raid and phase.
            for (var attempt = 0; ; attempt++)
            {
                ClientWebSocket active = null;
                try
                {
                    active = await GetConnectionAsync(token);
                    if (GameServer.Uid != accountUid) throw new OperationCanceledException("RAID_ACCOUNT_CHANGED", token);
                    var pending = new PendingRequest(active);
                    if (!requests.TryAdd(id, pending)) throw new InvalidOperationException("RAID_REQUEST_ALREADY_PENDING");
                    try
                    {
                        await SendAsync(active, request, token);
                        var reply = await WaitAsync(pending.Completion.Task, token);
                        if ((string)reply["code"] != "SUCCESS") throw new GameServerException((string)reply["code"] ?? "RAID_REQUEST_FAILED", 0, id);
                        return reply["data"].ToObject<T>();
                    }
                    finally { requests.TryRemove(id, out _); }
                }
                catch (Exception error) when (attempt == 0 && !(error is GameServerException) && !(error is OperationCanceledException))
                {
                    // Another request may already have connected a replacement.
                    // Only abort the transport used by this failed attempt.
                    AbortTransport(active);
                    await Task.Delay(250, token);
                }
            }
        }

        public async Task CloseAsync(CancellationToken token)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(RaidSocketClient));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
            token = linked.Token;
            await connectionGate.WaitAsync(token);
            try
            {
                var active = socket;
                if (active == null || (active.State != WebSocketState.Open && active.State != WebSocketState.CloseReceived)) return;
                await sendGate.WaitAsync(token);
                try { await active.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "connection refresh", token); }
                finally { sendGate.Release(); }
                // The existing receive loop consumes the peer close frame; do not run a second receiver.
                for (var attempt = 0; attempt < 100 && active.State == WebSocketState.CloseSent; attempt++) await Task.Delay(50, token);
                authenticated = false;
                if (active.State == WebSocketState.CloseSent) throw new TimeoutException("RAID_CLOSE_HANDSHAKE_TIMEOUT");
            }
            finally { connectionGate.Release(); }
        }

        private async Task SendAsync(ClientWebSocket active, JObject message, CancellationToken token)
        {
            var bytes = Encoding.UTF8.GetBytes(message.ToString(Formatting.None));
            await sendGate.WaitAsync(token);
            try { await active.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token); }
            finally { sendGate.Release(); }
        }

        private static void AbortTransport(ClientWebSocket active)
        {
            try { active?.Abort(); }
            catch (ObjectDisposedException) { } // A reconnect may already have disposed this old transport.
        }

        private static async Task<JObject> WaitAsync(Task<JObject> task, CancellationToken token)
        {
            var timeout = Task.Delay(10000, token);
            if (await Task.WhenAny(task, timeout) != task) { token.ThrowIfCancellationRequested(); throw new TimeoutException("RAID_REQUEST_TIMEOUT"); }
            return await task;
        }

        private async Task ReceiveAsync(ClientWebSocket active, TaskCompletionSource<JObject> activeAuthentication, CancellationToken token)
        {
            var buffer = new byte[8192];
            try
            {
                // A transport may already report Closed while a final message and
                // close frame remain buffered. Consume the terminal frame/error.
                while (!token.IsCancellationRequested)
                {
                    using (var content = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await active.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                            if (result.MessageType == WebSocketMessageType.Close) throw new IOException("RAID_SOCKET_CLOSED");
                            content.Write(buffer, 0, result.Count);
                            if (content.Length > 2 * 1024 * 1024) throw new IOException("RAID_MESSAGE_TOO_LARGE");
                        } while (!result.EndOfMessage);
                        var message = JObject.Parse(Encoding.UTF8.GetString(content.ToArray()));
                        var type = (string)message["type"];
                        if (type == "AUTH_OK" || type == "AUTH_FAIL") activeAuthentication.TrySetResult(message);
                        else if (type == "RAID_ROUND") { if (ReferenceEquals(socket, active)) broadcasts.Enqueue(message); }
                        else if (message["request_id"] != null && requests.TryGetValue((string)message["request_id"], out var request)
                            && ReferenceEquals(request.Socket, active)) request.Completion.TrySetResult(message);
                    }
                }
                token.ThrowIfCancellationRequested();
            }
            catch (Exception error)
            {
                if (ReferenceEquals(socket, active))
                {
                    authenticated = false;
                }
                activeAuthentication.TrySetException(error);
                foreach (var pending in requests.Values)
                    if (ReferenceEquals(pending.Socket, active)) pending.Completion.TrySetException(error);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            lifetime.Cancel();
            socket?.Abort();
            socket?.Dispose();
            foreach (var request in requests.Values) request.Completion.TrySetCanceled();
            lifetime.Dispose();
        }
    }
}
