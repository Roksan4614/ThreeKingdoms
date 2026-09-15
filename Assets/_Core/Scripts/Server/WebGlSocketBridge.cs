#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace ThreeKingdoms.Client.Server
{
    // Same .jslib -> SendMessage pattern as MessageHandler, on an owned persistent
    // object. No scene, UI, authentication, or raid protocol is implemented here.
    [Preserve]
    public sealed class WebGlSocketBridge : MonoBehaviour
    {
        private const string ObjectName = "__ThreeKingdomsWebGlSockets";
        private static WebGlSocketBridge instance;
        private BrowserTransport transport;

        internal static IBrowserSocketTransport Transport
        {
            get
            {
                if (instance == null)
                {
                    var context = SynchronizationContext.Current;
                    if (context == null) throw new InvalidOperationException("Create WebGL sockets on Unity's main thread.");
                    var owner = new GameObject(ObjectName);
                    instance = owner.AddComponent<WebGlSocketBridge>();
                    DontDestroyOnLoad(owner);
                    instance.transport = new BrowserTransport(context, Thread.CurrentThread.ManagedThreadId);
                }
                return instance.transport;
            }
        }

        [Preserve]
        public void OnSocketEvent(string json)
        {
            WireEvent message;
            try { message = JsonConvert.DeserializeObject<WireEvent>(json); }
            catch (JsonException) { Debug.LogWarning("WEBGL_SOCKET_CALLBACK_INVALID"); return; }
            if (message == null || transport == null) return;
            transport.Deliver(message);
        }
        private void OnDestroy()
        {
            transport?.Shutdown();
            if (instance == this) instance = null;
        }

        [Preserve]
        private sealed class WireEvent
        {
            [Preserve] public int id;
            [Preserve] public string type;
            [Preserve] public string text;
            [Preserve] public int code;
            [Preserve] public bool clean;
        }

        private sealed class BrowserTransport : IBrowserSocketTransport
        {
            private readonly Dictionary<int, Action<BrowserSocketEvent>> callbacks = new Dictionary<int, Action<BrowserSocketEvent>>();
            private readonly SynchronizationContext context;
            private readonly int mainThread;
            public BrowserTransport(SynchronizationContext context, int thread) { this.context = context; mainThread = thread; }
            public void Connect(int id, string url, Action<BrowserSocketEvent> callback)
            {
                EnsureMainThread(); callbacks.Add(id, callback);
                if (TKG_WebSocket_Connect(id, url, ObjectName) < 0)
                { callbacks.Remove(id); throw new WebSocketException("WEBGL_SOCKET_CONNECT_FAILED"); }
            }
            public void Send(int id, string text)
            {
                EnsureMainThread();
                if (TKG_WebSocket_Send(id, text) < 0) throw new WebSocketException("WEBGL_SOCKET_SEND_FAILED");
            }
            public void Close(int id, int code, string reason)
            {
                EnsureMainThread();
                if (TKG_WebSocket_Close(id, code, reason) < 0) throw new WebSocketException("WEBGL_SOCKET_CLOSE_FAILED");
            }
            public void Abort(int id) => Main(() => { callbacks.Remove(id); TKG_WebSocket_Abort(id); });
            public void Dispose(int id) => Main(() => { callbacks.Remove(id); TKG_WebSocket_Dispose(id); });
            public void Deliver(WireEvent wire)
            {
                if (!callbacks.TryGetValue(wire.id, out var callback)) return;
                var kind = wire.type == "open" ? BrowserSocketEventKind.Open
                    : wire.type == "message" ? BrowserSocketEventKind.Text
                    : wire.type == "close" ? BrowserSocketEventKind.Close : BrowserSocketEventKind.Error;
                callback(new BrowserSocketEvent { Kind = kind, Text = wire.text, CloseCode = wire.code, Clean = wire.clean });
                if (kind == BrowserSocketEventKind.Close) callbacks.Remove(wire.id);
            }
            public void Shutdown()
            {
                foreach (var entry in callbacks.ToArray())
                {
                    TKG_WebSocket_Dispose(entry.Key);
                    entry.Value(new BrowserSocketEvent { Kind = BrowserSocketEventKind.Error, Text = "WEBGL_SOCKET_BRIDGE_CLOSED" });
                }
                callbacks.Clear();
            }
            private void EnsureMainThread()
            {
                if (Thread.CurrentThread.ManagedThreadId != mainThread) throw new InvalidOperationException("WebGL socket I/O must run on Unity's main thread.");
            }
            private void Main(Action action)
            {
                if (Thread.CurrentThread.ManagedThreadId == mainThread) action();
                else context.Post(_ => action(), null);
            }
        }

        [DllImport("__Internal")] private static extern int TKG_WebSocket_Connect(int id, string url, string target);
        [DllImport("__Internal")] private static extern int TKG_WebSocket_Send(int id, string text);
        [DllImport("__Internal")] private static extern int TKG_WebSocket_Close(int id, int code, string reason);
        [DllImport("__Internal")] private static extern void TKG_WebSocket_Abort(int id);
        [DllImport("__Internal")] private static extern void TKG_WebSocket_Dispose(int id);
    }
}
#endif
