using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AutoOrderInLayerWorker
{
#if UNITY_EDITOR
    [MenuItem("Rev9/AutoOrderInLayer")]
    static void Run()
    {
        Utils.ClearDebugLog();
        float startTime = Time.realtimeSinceStartup;

        // 1. 현재 씬에 있는 객체들 처리
        var sceneTargets = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                                     .OfType<IValidatable>();

        foreach (var target in sceneTargets)
        {
            target.OnManualValidate();
            EditorUtility.SetDirty(target as MonoBehaviour);
        }

        // 2. 프로젝트 내 모든 프리팹 에셋 처리
        // "t:Prefab" 필터를 사용해 모든 프리팹의 GUID를 가져옵니다.
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            GameObject contentsRoot = PrefabUtility.LoadPrefabContents(path);
            if (contentsRoot == null) continue;

            bool isModified = false;

            // 프리팹 내부에서 IValidatable을 가진 모든 컴포넌트 찾기
            var components = contentsRoot.GetComponentsInChildren<MonoBehaviour>(true)
                                         .OfType<AutoOrderInLayer>();

            foreach (var comp in components)
            {
                comp.OnManualValidate();
                isModified = true;
            }

            if (isModified)
            {
                // 변경사항을 명시적으로 프리팹 에셋에 저장
                PrefabUtility.SaveAsPrefabAsset(contentsRoot, path);
            }

            // 메모리 해제
            PrefabUtility.UnloadPrefabContents(contentsRoot);
        }

        // 최종 변경사항 물리적 저장
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"VALIDATE FINISHED: {(Time.realtimeSinceStartup - startTime):0.#0}s");

    }
#endif
}
