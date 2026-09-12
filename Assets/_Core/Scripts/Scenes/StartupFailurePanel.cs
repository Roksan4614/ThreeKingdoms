using System;
using Cysharp.Threading.Tasks;
using ThreeKingdoms.Client.Server;
using UnityEngine;

// Available before the Addressable popup assets are ready; the usual modal is preferred when available.
public sealed class StartupFailurePanel : MonoBehaviour
{
    const string Message = "게임을 시작하지 못했습니다.\n다시 시도해 주세요.";
    bool retrying;
    PopupModalComponent modal;
    Rect window;
    string stage;
    public bool IsRetrying => retrying;

    public static void Show(MonoBehaviour owner, Exception error)
    {
        if (owner == null || error is OperationCanceledException) return;
        try { GameServer.Report(error); } catch (Exception listenerError) { Debug.LogException(listenerError); }
        var panel = owner.GetComponent<StartupFailurePanel>() ?? owner.gameObject.AddComponent<StartupFailurePanel>();
        StopBackgroundWork();
        panel.stage = owner.GetType().Name;
        panel.retrying = false;
        panel.window = new Rect((Screen.width - Math.Min(420, Screen.width - 30)) / 2f, (Screen.height - 170) / 2f,
            Math.Min(420, Screen.width - 30), 170);
        Debug.LogError($"[STARTUP_FAILED] stage={panel.stage} error={error.GetType().Name}");
        panel.TryModalAsync().Forget(exception => Debug.LogWarning("[STARTUP_MODAL] " + exception.Message));
    }

    async UniTask TryModalAsync()
    {
        var popup = FindFirstObjectByType<PopupManager>();
        if (popup == null) return;
        try
        {
            await popup.ShowDimmAsync(false, false, _durationWait: 0);
            var loaded = await popup.OpenPopupAsync<PopupModalComponent>(PopupType.Modal,
                new PopupModalComponent.ModalPopupData { content = Message, confirm = "다시 시도", cancel = "닫기" });
            if (!this || retrying) { loaded?.Close(); return; }
            modal = loaded;
            if (loaded == null) return;
            await UniTask.WaitUntil(() => loaded == null || !loaded.gameObject.activeSelf, cancellationToken: destroyCancellationToken);
            // Close destroys the Unity object. Its managed status remains readable, as in PopupManager.OpenModalAsync.
            var retry = loaded.statusType == StatusType.Success;
            if (ReferenceEquals(modal, loaded)) modal = null;
            if (retry) Retry();
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { modal = null; Debug.LogWarning("[STARTUP_MODAL] " + error.Message); }
    }

    public void Retry()
    {
        if (retrying) return;
        retrying = true;
        RetryAsync().Forget(error =>
        {
            retrying = false;
            try { GameServer.Report(error); } catch (Exception listenerError) { Debug.LogException(listenerError); }
        });
    }

    async UniTask RetryAsync()
    {
        Debug.Log($"[STARTUP_RETRY] stage={stage}");
        StopBackgroundWork();
        TutorialManager.instance.ResetForBootstrap();
        await GameServer.ResetConnectionAsync();
        var popup = FindFirstObjectByType<PopupManager>();
        popup?.CloseAll();
        Time.timeScale = 1;
        LoadSceneManager.instance.RestartApp();
    }

    static void StopBackgroundWork()
    {
        try { DataManager.Release(); } catch (Exception error) { Debug.LogException(error); }
        try { TimeManager.instance?.Stop(); } catch (Exception error) { Debug.LogException(error); }
    }

    void OnGUI()
    {
        if (modal != null && modal.gameObject.activeSelf) return;
        GUI.depth = -1000;
        window = GUILayout.Window(GetInstanceID(), window, _ =>
        {
            GUILayout.Space(12);
            GUILayout.Label(retrying ? "다시 연결하는 중입니다." : Message);
            GUILayout.Space(15);
            GUI.enabled = !retrying;
            if (GUILayout.Button("다시 시도", GUILayout.Height(38))) Retry();
            GUI.enabled = true;
        }, "게임 시작");
    }
}
