using UnityEditor;
using UnityEngine;

public class EditorWindow_Build : EditorWindow
{
    [MenuItem("Rev9/PlayerPrefabs RESET")]
    public static void ResetPlayerPrefabs()
    {
        PlayerPrefs.DeleteAll();
    }
}
