using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreeKingdoms.Client.Server
{
    internal enum BrowserSocketEventKind { Open, Text, Error, Close }
    internal sealed class BrowserSocketEvent
    {
        public BrowserSocketEventKind Kind;
        public string Text;
        public int CloseCode;
        public bool Clean;
    }
    internal interface IBrowserSocketTransport
    {
        void Connect(int id, string url, Action<BrowserSocketEvent> callback);
        void Send(int id, string text);
        void Close(int id, int code, string reason);
        void Abort(int id);
        void Dispose(int id);
    }

    // Uses browser WebSocket I/O. The BCL websocket types below are only enums,
    // receive-result data, and exceptions; ClientWebSocket is never constructed.
    public sealed class WebGlClientWebSocket : IDisposable
    {
        internal const int MaxMessageBytes = 2 * 1024 * 1024;
        private const int MaxQueuedBytes = 2 * MaxMessageBytes;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private static int nextId;
        private readonly int id;
        private readonly object sync = new object();
        private readonly IBrowserSocketTransport transport;
        private readonly Queue<Frame> incoming = new Queue<Frame>();
        private WebSocketState state;
        private Exception failure;
        private bool disposed, peerClosed;
        private int queuedBytes;
        private WebSocketCloseStatus? closeStatus;
        private string closeDescription;
        private TaskCompletionSource<bool> connecting, closing;
        private TaskCompletionSource<WebSocketReceiveResult> receiving;
        private ArraySegment<byte> receiveBuffer;
        private MemoryStream outgoing;

        private sealed class Frame
        {
            public byte[] Bytes;
            public int Offset;
            public bool Close;
        }

        public WebGlClientWebSocket() : this(CreateTransport()) { }
        internal WebGlClientWebSocket(IBrowserSocketTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            id = Interlocked.Increment(ref nextId);
            if (id <= 0) throw new InvalidOperationException("WebGL socket handle space exhausted.");
        }
        private static IBrowserSocketTransport CreateTransport()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return WebGlSocketBridge.Transport;
#else
            throw new PlatformNotSupportedException("WebGlClientWebSocket is for the WebGL player only.");
#endif
        }
        public WebSocketState State { get { lock (sync) return state; } }
        public WebSocketCloseStatus? CloseStatus { get { lock (sync) return closeStatus; } }
        public string CloseStatusDescription { get { lock (sync) return closeDescription; } }

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (uri.Scheme != "ws" && uri.Scheme != "wss") throw new ArgumentException("A ws/wss URI is required.", nameof(uri));
            cancellationToken.ThrowIfCancellationRequested();
            Task task;
            lock (sync)
            {
                ThrowIfDisposed();
                if (state != WebSocketState.None) throw new InvalidOperationException("A socket instance connects only once.");
                state = WebSocketState.Connecting;
                connecting = Completion<bool>();
                task = connecting.Task;
            }
            try { transport.Connect(id, uri.AbsoluteUri, OnEvent); }
            catch (Exception error) { Fail(error); }
            return WaitAsync(task, cancellationToken);
        }

        public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (buffer.Array == null) throw new ArgumentNullException(nameof(buffer));
            if (messageType != WebSocketMessageType.Text) throw new NotSupportedException("The raid browser transport accepts UTF-8 text messages only.");
            string text;
            lock (sync)
            {
                ThrowIfDisposed();
                if (state != WebSocketState.Open || peerClosed) throw new WebSocketException("WEBGL_SOCKET_NOT_OPEN");
                if ((outgoing?.Length ?? 0) + buffer.Count > MaxMessageBytes) throw new WebSocketException("WEBGL_MESSAGE_TOO_LARGE");
                outgoing ??= new MemoryStream();
                outgoing.Write(buffer.Array, buffer.Offset, buffer.Count);
                if (!endOfMessage) return Task.CompletedTask;
                var bytes = outgoing.ToArray();
                outgoing.Dispose(); outgoing = null;
                text = Utf8.GetString(bytes);
                if (text.IndexOf('\0') >= 0) throw new ArgumentException("Raw NUL is not valid in a JSON websocket message.", nameof(buffer));
            }
            try { transport.Send(id, text); }
            catch (Exception error) { Fail(error); return Task.FromException(error); }
            return Task.CompletedTask;
        }

        public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (buffer.Array == null || buffer.Count == 0) throw new ArgumentException("A non-empty receive buffer is required.", nameof(buffer));
            Task<WebSocketReceiveResult> task;
            lock (sync)
            {
                ThrowIfDisposed();
                if (failure != null) return Task.FromException<WebSocketReceiveResult>(failure);
                if (state == WebSocketState.None || state == WebSocketState.Connecting) throw new InvalidOperationException("Connect before receiving.");
                if (receiving != null) throw new InvalidOperationException("Only one receiver is allowed.");
                if (TryRead(buffer, out var result)) return Task.FromResult(result);
                receiving = Completion<WebSocketReceiveResult>(); receiveBuffer = buffer; task = receiving.Task;
            }
            return WaitAsync(task, cancellationToken);
        }

        public Task CloseOutputAsync(WebSocketCloseStatus status, string description, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var code = (int)status;
            if (code != 1000 && (code < 3000 || code > 4999)) throw new ArgumentException("Browsers allow outgoing close code 1000 or 3000..4999.", nameof(status));
            description ??= "";
            if (Utf8.GetByteCount(description) > 123 || description.IndexOf('\0') >= 0) throw new ArgumentException("Close reason exceeds the browser limit.", nameof(description));
            lock (sync)
            {
                ThrowIfDisposed();
                if (state == WebSocketState.Closed) return Task.CompletedTask;
                if (peerClosed) { state = WebSocketState.Closed; closing?.TrySetResult(true); return Task.CompletedTask; }
                if (state == WebSocketState.CloseSent) return Task.CompletedTask;
                if (state != WebSocketState.Open) throw new WebSocketException("WEBGL_SOCKET_NOT_OPEN");
                state = WebSocketState.CloseSent;
            }
            try { transport.Close(id, code, description); }
            catch (Exception error) { Fail(error); return Task.FromException(error); }
            return Task.CompletedTask;
        }

        public async Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken cancellationToken)
        {
            Task task;
            lock (sync)
            {
                ThrowIfDisposed();
                if (state == WebSocketState.Closed) return;
                closing ??= Completion<bool>(); task = closing.Task;
            }
            await CloseOutputAsync(status, description, cancellationToken);
            await WaitAsync(task, cancellationToken);
            lock (sync) { if (peerClosed) state = WebSocketState.Closed; }
        }

        public void Abort() => Fail(new WebSocketException("WEBGL_SOCKET_ABORTED"));
        public void Dispose()
        {
            lock (sync)
            {
                if (disposed) return;
                disposed = true;
                if (state != WebSocketState.Closed) Terminate(new ObjectDisposedException(nameof(WebGlClientWebSocket)));
                incoming.Clear(); queuedBytes = 0; outgoing?.Dispose(); outgoing = null;
            }
            transport.Dispose(id);
        }

        private void OnEvent(BrowserSocketEvent message)
        {
            lock (sync)
            {
                if (disposed || state == WebSocketState.Aborted || peerClosed) return;
                switch (message.Kind)
                {
                    case BrowserSocketEventKind.Open:
                        if (state == WebSocketState.Connecting) { state = WebSocketState.Open; connecting?.TrySetResult(true); }
                        break;
                    case BrowserSocketEventKind.Text:
                        var bytes = Utf8.GetBytes(message.Text ?? "");
                        if (bytes.Length > MaxMessageBytes || queuedBytes + bytes.Length > MaxQueuedBytes)
                        { Fail(new WebSocketException("WEBGL_RECEIVE_LIMIT_EXCEEDED")); return; }
                        incoming.Enqueue(new Frame { Bytes = bytes }); queuedBytes += bytes.Length;
                        DispatchReceive();
                        break;
                    case BrowserSocketEventKind.Error:
                        Fail(new WebSocketException(message.Text ?? "WEBGL_SOCKET_ERROR"));
                        break;
                    case BrowserSocketEventKind.Close:
                        peerClosed = true; closeStatus = (WebSocketCloseStatus)message.CloseCode; closeDescription = message.Text ?? "";
                        if (state == WebSocketState.Connecting)
                        { Terminate(new WebSocketException("WEBGL_CONNECT_CLOSED")); return; }
                        // CloseOutputAsync has an independent receive loop. Keep
                        // it readable until queued text and the close frame drain.
                        incoming.Enqueue(new Frame { Close = true });
                        if (message.Clean) closing?.TrySetResult(true);
                        else closing?.TrySetException(new WebSocketException("WEBGL_UNCLEAN_CLOSE"));
                        DispatchReceive();
                        break;
                }
            }
        }
        private bool TryRead(ArraySegment<byte> buffer, out WebSocketReceiveResult result)
        {
            if (incoming.Count == 0 && !peerClosed) { result = null; return false; }
            if (incoming.Count == 0 || incoming.Peek().Close)
            {
                if (incoming.Count > 0) incoming.Dequeue();
                state = state == WebSocketState.CloseSent || state == WebSocketState.Closed
                    ? WebSocketState.Closed : WebSocketState.CloseReceived;
                result = new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, closeStatus, closeDescription);
                return true;
            }
            var frame = incoming.Peek();
            var count = Math.Min(buffer.Count, frame.Bytes.Length - frame.Offset);
            Buffer.BlockCopy(frame.Bytes, frame.Offset, buffer.Array, buffer.Offset, count);
            frame.Offset += count; queuedBytes -= count;
            var end = frame.Offset == frame.Bytes.Length;
            if (end) incoming.Dequeue();
            result = new WebSocketReceiveResult(count, WebSocketMessageType.Text, end);
            return true;
        }
        private void DispatchReceive()
        {
            if (receiving == null || !TryRead(receiveBuffer, out var result)) return;
            var completed = receiving; receiving = null; receiveBuffer = default;
            completed.TrySetResult(result);
        }
        private void Fail(Exception error)
        {
            lock (sync)
            {
                if (disposed || state == WebSocketState.Aborted || state == WebSocketState.Closed) return;
                Terminate(error);
            }
            transport.Abort(id);
        }
        private void Terminate(Exception error)
        {
            failure = error; state = WebSocketState.Aborted;
            incoming.Clear(); queuedBytes = 0; outgoing?.Dispose(); outgoing = null;
            if (error is OperationCanceledException canceled)
            {
                connecting?.TrySetCanceled(canceled.CancellationToken);
                receiving?.TrySetCanceled(canceled.CancellationToken);
                closing?.TrySetCanceled(canceled.CancellationToken);
            }
            else
            {
                connecting?.TrySetException(error); receiving?.TrySetException(error); closing?.TrySetException(error);
            }
            receiving = null; receiveBuffer = default;
        }
        private async Task WaitAsync(Task task, CancellationToken token)
        {
            using (token.Register(() => { if (!task.IsCompleted) Fail(new OperationCanceledException(token)); })) await task;
        }
        private async Task<T> WaitAsync<T>(Task<T> task, CancellationToken token)
        {
            using (token.Register(() => { if (!task.IsCompleted) Fail(new OperationCanceledException(token)); })) return await task;
        }
        private static TaskCompletionSource<T> Completion<T>() => new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        private void ThrowIfDisposed() { if (disposed) throw new ObjectDisposedException(nameof(WebGlClientWebSocket)); }
    }
}
