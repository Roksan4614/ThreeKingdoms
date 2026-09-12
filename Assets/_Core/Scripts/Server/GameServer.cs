using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using ThreeKingdoms.Shared.Rest;
using ThreeKingdoms.Shared.Types;

namespace ThreeKingdoms.Client.Server
{
    [Serializable]
    public sealed class ServerIntegrationSettings
    {
        public bool UseServer = true;
        public string ApiBaseUrl = "http://127.0.0.1:11080";
        public string TableBaseUrl = "http://127.0.0.1:11080/table_data/json/";
        public string RaidUrl = "ws://127.0.0.1:11083/";
        public long TestUid;
        public string ApiSecretKey;
    }

    public sealed class GameServerException : Exception
    {
        public string Code { get; }
        public long HttpStatus { get; }
        public string RequestId { get; }
        public new JToken Data { get; }
        public GameServerException(string code, long status, string requestId = null, JToken data = null)
            : base(code) { Code = code; HttpStatus = status; RequestId = requestId; Data = data; }
    }

    // 전송/세션/서버 테이블의 단일 소유자. UI 상태 적용은 ServerState가 담당한다.
    public static class GameServer
    {
        private static ServerIntegrationSettings _settings;
        private static readonly SemaphoreSlim InitializeGate = new SemaphoreSlim(1, 1);
        private static readonly Dictionary<string, JArray> Tables = new Dictionary<string, JArray>();
        private static bool _initialized;
        private static UnityRestClient _client;
        public static ServerIntegrationSettings Settings
        {
            get
            {
                if (_settings != null) return _settings;
#if UNITY_EDITOR
                var file = Path.GetFullPath(Path.Combine(Application.dataPath, "../UserSettings/ServerIntegration.json"));
#else
                var file = Path.Combine(Application.persistentDataPath, "ServerIntegration.json");
#endif
                _settings = File.Exists(file) ? JsonConvert.DeserializeObject<ServerIntegrationSettings>(File.ReadAllText(file)) : new ServerIntegrationSettings();
                return _settings;
            }
        }
        public static bool Enabled => Settings.UseServer;
        public static bool IsLoggedIn => Uid > 0 && !string.IsNullOrEmpty(SessionKey);
        public static long Uid { get; private set; }
        internal static string SessionKey { get; private set; }
        public static string TableVersion { get; private set; }
        public static LoginRes Login { get; private set; }
        public static string LastError { get; private set; }
        public static event Action<string> Failed;
        public static event Action<string, object, JToken, string> RequestSucceeded;
        internal static void NotifyRequestSucceeded(string path, object request, JToken data, string requestId)
        {
            var handlers = RequestSucceeded;
            if (handlers == null) return;
            foreach (Action<string, object, JToken, string> handler in handlers.GetInvocationList())
            {
                try { handler(path, request, data, requestId); }
                catch (Exception error) { Debug.LogWarning("[SERVER_UI_LISTENER] " + error.Message); }
            }
        }
        public static RestPublicService Public { get; private set; }
        public static RestUserService User { get; private set; }
        public static RestCurrencyService Currency { get; private set; }
        public static RestCharacterService Character { get; private set; }
        public static RestCastleService Castle { get; private set; }
        public static RestDailyDungeonService DailyDungeon { get; private set; }
        public static RestGuideQuestService GuideQuest { get; private set; }
        public static RestContentShopService ContentShop { get; private set; }
        public static RestItemService Item { get; private set; }
        public static RestTournamentService Tournament { get; private set; }
        public static RestRaidService Raid { get; private set; }

        public static RestRequestOptions Options() => new RestRequestOptions
        {
            ClientVersion = Application.version,
            RequestId = Guid.NewGuid().ToString(),
            TableVersion = TableVersion
        };

        public static JArray TableRows(string name)
        {
            if (!Tables.TryGetValue(name, out var rows)) throw new InvalidOperationException("SERVER_TABLE_NOT_LOADED: " + name);
            return rows;
        }

        public static bool HasTable(string name) => Tables.ContainsKey(name);

        public static async UniTask InitializeAsync(CancellationToken token = default)
        {
            if (!Enabled || _initialized) return;
            Application.runInBackground = true;
            await InitializeGate.WaitAsync(token);
            try
            {
                if (_initialized) return;
                if (Settings.TestUid <= 0 || string.IsNullOrWhiteSpace(Settings.ApiSecretKey)) throw new GameServerException("DEV_SERVER_SETTINGS_REQUIRED", 0);
                var manifest = JObject.Parse(Encoding.UTF8.GetString(await DownloadAsync(Settings.TableBaseUrl.TrimEnd('/') + "/table_hash.json", token)));
                var next = new Dictionary<string, JArray>();
                foreach (var entry in ((JObject)manifest["tables"]).Properties())
                {
                    var bytes = await DownloadAsync(Settings.TableBaseUrl.TrimEnd('/') + "/" + entry.Name + ".json", token);
                    string hash;
                    using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                    if (hash != (string)entry.Value) throw new GameServerException("TABLE_DOWNLOAD_HASH_MISMATCH", 0);
                    next.Add(entry.Name, JArray.Parse(Encoding.UTF8.GetString(bytes)));
                }
                Tables.Clear();
                foreach (var entry in next) Tables.Add(entry.Key, entry.Value);
                TableVersion = (string)manifest["total_hash"];
                _client = new UnityRestClient();
                Public = new RestPublicService(_client);
                User = new RestUserService(_client);
                Currency = new RestCurrencyService(_client);
                Character = new RestCharacterService(_client);
                Castle = new RestCastleService(_client);
                DailyDungeon = new RestDailyDungeonService(_client);
                GuideQuest = new RestGuideQuestService(_client);
                ContentShop = new RestContentShopService(_client);
                Item = new RestItemService(_client);
                Tournament = new RestTournamentService(_client);
                Raid = new RestRaidService(_client);
                _initialized = true;
                Debug.Log($"[SERVER_TABLES] version={TableVersion} tables={Tables.Count}");
            }
            catch (Exception error) { Report(error); throw; }
            finally { InitializeGate.Release(); }
        }

        public static async UniTask<LoginRes> LoginAsync(CancellationToken token = default)
        {
            await InitializeAsync(token);
            var response = await Public.LoginAsync(new LoginReq
            {
                TestUid = Settings.TestUid,
                OsType = ThreeKingdoms.Shared.Enums.OsType.Webgl,
                OsName = Application.platform.ToString(),
                OsVersion = SystemInfo.operatingSystem.Substring(0, Math.Min(64, SystemInfo.operatingSystem.Length))
            }, Options(), token);
            Login = response.Data ?? throw new GameServerException("LOGIN_DATA_MISSING", 200);
            Uid = Login.UserInfo.Uid;
            SessionKey = Login.UserInfo.SessionKey;
            Debug.Log($"[SERVER_LOGIN] uid={Uid} region_selected={Login.RegionSelected}");
            return Login;
        }

        public static void ClearError() => LastError = null;
        public static async UniTask ResetConnectionAsync()
        {
            await InitializeGate.WaitAsync();
            try
            {
                if (_client != null) await _client.StopAsync();
                _initialized = false;
                _settings = null;
                _client = null;
                Uid = 0; SessionKey = null; Login = null; TableVersion = null;
                Tables.Clear();
                ClearError();
                ServerState.Reset();
            }
            finally { InitializeGate.Release(); }
        }

        public static void Report(Exception error)
        {
            if (error is OperationCanceledException) return;
            LastError = error is GameServerException server ? server.Code : error.GetType().Name + ": " + error.Message;
            Debug.LogError("[SERVER_ERROR] " + LastError);
            Failed?.Invoke(LastError);
        }

        public static async UniTask<byte[]> DownloadAsync(string url, CancellationToken token = default)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 20;
                await SendAsync(request, token);
                if (request.result != UnityWebRequest.Result.Success) throw new GameServerException("TABLE_DOWNLOAD_FAILED", request.responseCode);
                return request.downloadHandler.data;
            }
        }

        internal static async UniTask SendAsync(UnityWebRequest request, CancellationToken token)
        {
            var operation = request.SendWebRequest();
            using (token.Register(request.Abort))
            {
                while (!operation.isDone) await UniTask.Yield(PlayerLoopTiming.Update, token);
                token.ThrowIfCancellationRequested();
            }
        }
    }

    public sealed class UnityRestClient : IRestClient
    {
        private readonly SemaphoreSlim _requests = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        public async UniTask StopAsync()
        {
            _lifetime.Cancel();
            await _requests.WaitAsync();
            _requests.Release();
        }
        public async UniTask<CommonBody<TRes>> PostAsync<TReq, TRes>(RestEndpoint endpoint, TReq request, RestRequestOptions options, CancellationToken token)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
            token = linked.Token;
            await _requests.WaitAsync(token);
            try
            {
                if (endpoint.RequiresSession && !GameServer.IsLoggedIn) throw new GameServerException("CLIENT_SESSION_REQUIRED", 0);
                var headers = new Dictionary<string, string>
                {
                    ["api-secret-key"] = GameServer.Settings.ApiSecretKey,
                    ["client-version"] = options.ClientVersion,
                    ["x-request-id"] = options.RequestId,
                    ["x-table-version"] = options.TableVersion
                };
                foreach (var required in endpoint.Headers)
                    if (required.Required && (!headers.TryGetValue(required.Name, out var value) || string.IsNullOrEmpty(value)))
                        throw new GameServerException("CLIENT_HEADER_REQUIRED: " + required.Name, 0);
                using (var web = new UnityWebRequest(GameServer.Settings.ApiBaseUrl.TrimEnd('/') + endpoint.Path, "POST"))
                {
                    web.timeout = 20;
                    web.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)));
                    web.downloadHandler = new DownloadHandlerBuffer();
                    web.SetRequestHeader("Content-Type", "application/json");
                    foreach (var header in headers) if (!string.IsNullOrEmpty(header.Value)) web.SetRequestHeader(header.Key, header.Value);
                    if (endpoint.RequiresSession)
                    {
                        web.SetRequestHeader("uid", GameServer.Uid.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        web.SetRequestHeader("user-session-key", GameServer.SessionKey);
                    }
                    await GameServer.SendAsync(web, token);
                    if (web.responseCode == 0) throw new GameServerException("SERVER_CONNECTION_FAILED", 0, options.RequestId);
                    JObject body;
                    try { body = JObject.Parse(web.downloadHandler.text); }
                    catch (JsonException) { throw new GameServerException("SERVER_RESPONSE_INVALID", web.responseCode, options.RequestId); }
                    var code = (string)body["code"];
                    Debug.Log($"[SERVER_API] uid={GameServer.Uid} path={endpoint.Path} code={code} request_id={options.RequestId}");
                    if (web.responseCode < 200 || web.responseCode >= 300 || code != "SUCCESS")
                        throw new GameServerException(code ?? "SERVER_HTTP_ERROR", web.responseCode, (string)body["request_id"] ?? options.RequestId, body["data"]);
                    var result = body.ToObject<CommonBody<TRes>>();
                    GameServer.NotifyRequestSucceeded(endpoint.Path, request, body["data"], options.RequestId);
                    return result;
                }
            }
            catch (Exception error) { GameServer.Report(error); throw; }
            finally { _requests.Release(); }
        }
    }
}
