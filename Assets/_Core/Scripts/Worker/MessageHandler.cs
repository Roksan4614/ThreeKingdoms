using System.Runtime.InteropServices;
using UnityEngine;

public class MessageHandler : MonoSingleton<MessageHandler>
{

    public void Create() { }

#if UNITY_EDITOR || UNITY_WEBGL

    [DllImport("__Internal")]
    public static extern void FirebaseRefreshToken();

    [DllImport("__Internal")]
    public static extern void StartGame();

    [DllImport("__Internal")]
    public static extern void FirebaseSignOut();

    [DllImport("__Internal")]
    public static extern void UnityProgressCall(int index, float per);

    [DllImport("__Internal")]
    public static extern bool IsMobileBrowser();

    public void SetFirebaseTokenFailed(string _errMessage)
    {
        IngameLog.Add("MessageHandler: SetFirebaseTokenFailed: " + _errMessage);
    }

    public void CopyTextGUI(string _text)
    {
        GUIUtility.systemCopyBuffer = _text;
    }
#endif
}
