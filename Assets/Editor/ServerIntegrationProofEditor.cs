using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThreeKingdoms.Client.Server;
using UnityEditor;
using UnityEngine;

// Example marker (write atomically):
// {"request_id":"proof-001","operation":"snapshot","screenshot":true}
// {"request_id":"proof-002","operation":"exercise","steps":["grow","open_item","castle_assign","castle_collect"]}
// Exercise steps: assets/characters/castle/shop/daily/tournament/raid (reads), grow/open_item/buy_shop,
// castle_assign/castle_collect/castle_upgrade, office_start/office_claim, daily_enter/daily_settle,
// tournament_set_attack/tournament_set_defense/tournament_refresh/tournament_ranking.
// daily_enter enters the real dungeon scene; daily_settle submits its measured game state and leaves the result popup open.
[InitializeOnLoad]
public static class ServerIntegrationProofEditor
{
    private static readonly string DirectoryPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ServerIntegrationProof"));
    private static readonly string RequestPath = Path.Combine(DirectoryPath, "request.json");
    private static readonly string ActivePath = Path.Combine(DirectoryPath, "active.json");
    private static bool initialized;
    private static bool busy;
    private static CancellationTokenSource activeCancellation;

    static ServerIntegrationProofEditor()
    {
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += () => activeCancellation?.Cancel();
    }

    private static void Tick()
    {
        if (busy)
        {
            if (!EditorApplication.isPlaying) activeCancellation?.Cancel();
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!initialized)
        {
            initialized = true;
            if (File.Exists(ActivePath))
            {
                try
                {
                    var interrupted = JObject.Parse(File.ReadAllText(ActivePath));
                    interrupted["status"] = "failed"; interrupted["reason"] = "EDITOR_RELOAD_INTERRUPTED_PROOF";
                    interrupted["completed_at"] = DateTime.UtcNow.ToString("O");
                    ServerIntegrationProof.WriteJson(Path.Combine(DirectoryPath, "result.json"), interrupted);
                    File.Delete(ActivePath);
                }
                catch (Exception error) { Debug.LogError("[SERVER_PROOF] Could not record interrupted request: " + error.GetType().Name); return; }
            }
        }
        if (!File.Exists(RequestPath)) return;
        ServerIntegrationProof.Request request;
        try
        {
            request = JsonConvert.DeserializeObject<ServerIntegrationProof.Request>(File.ReadAllText(RequestPath));
            if (request == null || !Regex.IsMatch(request.request_id ?? "", @"^[A-Za-z0-9_-]{1,80}$")) throw new InvalidOperationException("REQUEST_ID_INVALID");
            var consumed = Path.Combine(DirectoryPath, "request-" + request.request_id + ".consumed.json");
            if (File.Exists(consumed))
            {
                File.Delete(RequestPath);
                ServerIntegrationProof.WriteJson(Path.Combine(DirectoryPath, "result.json"), Failure(request.request_id, "REQUEST_ID_ALREADY_CONSUMED"));
                return;
            }
            File.Move(RequestPath, consumed);
        }
        catch (Exception error)
        {
            if (File.Exists(RequestPath)) File.Move(RequestPath, Path.Combine(DirectoryPath, "invalid-" + Guid.NewGuid().ToString("N") + ".json"));
            ServerIntegrationProof.WriteJson(Path.Combine(DirectoryPath, "result.json"), Failure(null, "INVALID_REQUEST_MARKER:" + error.GetType().Name));
            return;
        }
        if (!EditorApplication.isPlaying)
        {
            ServerIntegrationProof.WriteJson(Path.Combine(DirectoryPath, "result.json"), Failure(request.request_id, "PLAYMODE_REQUIRED"));
            return;
        }
        busy = true;
        activeCancellation = new CancellationTokenSource();
        ServerIntegrationProof.WriteJson(ActivePath, new JObject { ["request_id"] = request.request_id, ["operation"] = request.operation, ["started_at"] = DateTime.UtcNow.ToString("O") });
        Execute(request);
    }

    private static async void Execute(ServerIntegrationProof.Request request)
    {
        try
        {
            var result = await ServerIntegrationProof.RunAsync(request, DirectoryPath, activeCancellation.Token);
            ServerIntegrationProof.WriteJson(Path.Combine(DirectoryPath, "result.json"), result);
            ServerIntegrationProof.WriteJson(Path.Combine(DirectoryPath, "result-" + request.request_id + ".json"), result);
            Debug.Log("[SERVER_PROOF] request=" + request.request_id + " status=" + (string)result["status"]);
        }
        catch (Exception error)
        {
            ServerIntegrationProof.WriteJson(Path.Combine(DirectoryPath, "result.json"), Failure(request.request_id, error.GetType().Name));
        }
        finally
        {
            if (File.Exists(ActivePath)) File.Delete(ActivePath);
            activeCancellation.Dispose(); activeCancellation = null; busy = false;
        }
    }

    private static JObject Failure(string id, string reason) => new JObject
    { ["request_id"] = id, ["status"] = "failed", ["reason"] = reason, ["completed_at"] = DateTime.UtcNow.ToString("O") };
}
