using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class Editor_Menu
{
    [MenuItem("Rev9/Build/Open Window")]
    static void OpenWindow()
    {
        EditorWindow_Build window = EditorWindow.GetWindow<EditorWindow_Build>("Window :: Build");
    }

    [MenuItem("Rev9/PlayerPrefabs RESET", false, 10000)]
    static void ResetPlayerPrefabs()
    {
        PlayerPrefs.DeleteAll();
    }

    [MenuItem("Rev9/Restart UNITY", false, 10000)]
    static void RestartUnity()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorApplication.OpenProject(System.IO.Directory.GetCurrentDirectory());
    }

    [MenuItem("Rev9/UTIL/HASH_KEY")]
    public static void GetHashKey()
    {
        List<string> hashes = new();
        hashes.Add(Convert.ToBase64String(HexStringToByteArray("79:6f:bd:24:5a:f3:e4:1c:9b:71:d7:0a:27:5c:b7:8f:dd:88:a8:7f")));

        hashes.Add(Convert.ToBase64String(HexStringToByteArray("7a:6c:c5:1c:28:19:dc:bd:09:a6:39:2d:3a:a3:4c:dc:c6:a5:61:8b")));

        Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(hashes));
    }

    // 16진수 문자열을 바이트 배열로 변환하는 헬퍼 함수
    public static byte[] HexStringToByteArray(string _hex)
    {
        _hex = _hex.Replace(":", "");
        int numberChars = _hex.Length;
        byte[] bytes = new byte[numberChars / 2];
        for (int i = 0; i < numberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(_hex.Substring(i, 2), 16);
        return bytes;
    }

    [MenuItem("Rev9/Build/BUILD_ANDROID_DEV")]
    static void JenkinsBuild()
    {
        JenkinsBuildStart(BuildTarget.Android, true);
    }

    static void JenkinsBuildStart(BuildTarget _buildType, bool _isDev = true, bool _isStore = false, bool _isAPK = true)
    {

        EditorWindow_Build window = new();

        EditorWindow_Build.UserData userData = new();
        {
            userData.buildTarget = _buildType;
            userData.serviceType = _isDev ? ServiceType.Dev : ServiceType.Live;
            userData.isStore = _isStore;

            var fildData = Resources.Load<TextAsset>("EditorData/BuildData");
            userData.seasonIndex = fildData == null ? 0 : Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(fildData.ToString())["seasonIndex"];

            var versionInfo = PlayerSettings.bundleVersion.Split(".");
            userData.version = string.Join(".", versionInfo.Take(2));

            if (_buildType == BuildTarget.Android)
            {
                userData.androidData.isAPK = _isAPK;
                userData.androidData.bundleVersionCode = PlayerSettings.Android.bundleVersionCode.ToString();
            }
        }

        window.SetUserData(userData);
        window.Run(true, true);
    }

    [MenuItem("Rev9/Color/Open Palette")]
    static void OpenPalette()
    {
        EditorWindow.GetWindow<EditorWindow_Color>("Eitor Palette");
    }
}
