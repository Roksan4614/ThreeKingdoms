var ThreeKingdomsWebSocketLibrary = {
  $TKGWebSockets: {
    entries: {},
    maxMessageBytes: 2 * 1024 * 1024,
    maxBufferedBytes: 4 * 1024 * 1024,
    emit: function (entry, type, text, code, clean) {
      if (entry.disposed || TKGWebSockets.entries[entry.id] !== entry) return;
      SendMessage(entry.target, 'OnSocketEvent', JSON.stringify({
        id: entry.id, type: type, text: text || '', code: code || 0, clean: !!clean
      }));
    },
    abort: function (id) {
      var entry = TKGWebSockets.entries[id];
      if (!entry) return;
      entry.disposed = true;
      delete TKGWebSockets.entries[id];
      var socket = entry.socket;
      if (!socket) return;
      socket.onopen = socket.onmessage = socket.onerror = socket.onclose = null;
      try { if (socket.readyState === 0 || socket.readyState === 1) socket.close(1000, ''); } catch (_) {}
    }
  },

  TKG_WebSocket_Connect: function (id, urlPointer, targetPointer) {
    if (id <= 0 || TKGWebSockets.entries[id]) return -1;
    var entry = { id: id, target: UTF8ToString(targetPointer), socket: null, disposed: false };
    TKGWebSockets.entries[id] = entry;
    try {
      var socket = new WebSocket(UTF8ToString(urlPointer));
      entry.socket = socket;
      socket.binaryType = 'arraybuffer';
      socket.onopen = function () { TKGWebSockets.emit(entry, 'open'); };
      socket.onmessage = function (event) {
        if (entry.disposed || TKGWebSockets.entries[id] !== entry) return;
        if (typeof event.data !== 'string' || lengthBytesUTF8(event.data) > TKGWebSockets.maxMessageBytes) {
          TKGWebSockets.emit(entry, 'error', typeof event.data === 'string' ? 'WEBGL_MESSAGE_TOO_LARGE' : 'WEBGL_TEXT_MESSAGE_REQUIRED');
          TKGWebSockets.abort(id);
          return;
        }
        TKGWebSockets.emit(entry, 'message', event.data);
      };
      socket.onerror = function () {
        if (entry.disposed || TKGWebSockets.entries[id] !== entry) return;
        TKGWebSockets.emit(entry, 'error', 'WEBGL_SOCKET_ERROR');
        TKGWebSockets.abort(id);
      };
      socket.onclose = function (event) {
        TKGWebSockets.emit(entry, 'close', event.reason, event.code, event.wasClean);
        if (TKGWebSockets.entries[id] === entry) {
          entry.disposed = true;
          delete TKGWebSockets.entries[id];
          socket.onopen = socket.onmessage = socket.onerror = socket.onclose = null;
        }
      };
      return 0;
    } catch (_) {
      TKGWebSockets.emit(entry, 'error', 'WEBGL_SOCKET_CONNECT_FAILED');
      TKGWebSockets.abort(id);
      return -1;
    }
  },
  TKG_WebSocket_Send: function (id, textPointer) {
    var entry = TKGWebSockets.entries[id];
    if (!entry || entry.disposed || entry.socket.readyState !== 1) return -1;
    var text = UTF8ToString(textPointer);
    var bytes = lengthBytesUTF8(text);
    if (bytes > TKGWebSockets.maxMessageBytes || entry.socket.bufferedAmount + bytes > TKGWebSockets.maxBufferedBytes) return -2;
    try { entry.socket.send(text); return 0; } catch (_) { return -1; }
  },
  TKG_WebSocket_Close: function (id, code, reasonPointer) {
    var entry = TKGWebSockets.entries[id];
    if (!entry || entry.disposed) return -1;
    var reason = UTF8ToString(reasonPointer);
    if ((code !== 1000 && (code < 3000 || code > 4999)) || lengthBytesUTF8(reason) > 123) return -2;
    try {
      if (entry.socket.readyState === 0 || entry.socket.readyState === 1) entry.socket.close(code, reason);
      return 0;
    } catch (_) { return -1; }
  },
  TKG_WebSocket_Abort: function (id) { TKGWebSockets.abort(id); },
  TKG_WebSocket_Dispose: function (id) { TKGWebSockets.abort(id); }
};

autoAddDeps(ThreeKingdomsWebSocketLibrary, '$TKGWebSockets');
mergeInto(LibraryManager.library, ThreeKingdomsWebSocketLibrary);
