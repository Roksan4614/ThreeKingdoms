using System.Linq;
using UnityEditor;
using UnityEngine;

public interface IValidatable
{
    void OnManualValidate();
}

/*

#region VALIDATE
public void OnManualValidate() => m_element.Initialize(transform);

[SerializeField, HideInInspector]
ElementData m_element;

[System.Serializable]
struct ElementData
{
    public void Initialize(Transform _transform)
    {
    }
}
#endregion VALIDATE

*/


#if UNITY_EDITOR
public static class ValidateWorker
{
    [MenuItem("Rev9/Validate RUN")]
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
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefabRoot == null) continue;

            // 프리팹 내부에서 IValidatable을 가진 모든 컴포넌트 수집
            var components = prefabRoot.GetComponentsInChildren<MonoBehaviour>(true)
                                       .OfType<IValidatable>();

            bool isDirty = false;

            foreach (var comp in components)
            {
                MonoBehaviour mb = comp as MonoBehaviour;
                if (mb == null) continue;

                // 이 컴포넌트가 연결된 원본 프리팹 자산(Asset)을 확인
                Object prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(mb.gameObject);

                if (prefabSource != null)
                {
                    string sourcePath = AssetDatabase.GetAssetPath(prefabSource);

                    // 자식 컴포넌트의 원본 에셋 경로가 현재 검사 중인 프리팹의 경로와 다르다면?
                    // -> 다른 프리팹(B)의 인스턴스이므로, 해당 프리팹 차례에서 처리하도록 스킵합니다.
                    if (sourcePath != path)
                    {
                        continue;
                    }
                }

                // 현재 프리팹(A)에 직접 속한 컴포넌트거나 오버라이드된 데이터만 실행
                comp.OnManualValidate();
                EditorUtility.SetDirty(mb);
                isDirty = true;
            }

            // 변경 사항이 있을 때만 루트 프리팹을 더티 체크
            if (isDirty)
            {
                EditorUtility.SetDirty(prefabRoot);
            }
        }

        // 최종 변경사항 물리적 저장
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"VALIDATE FINISHED: {(Time.realtimeSinceStartup - startTime):0.#0}s");
    }
    //static void Run()
    //{
    //    Utils.ClearDebugLog();
    //    float startTime = Time.realtimeSinceStartup;

    //    // 1. 현재 씬에 있는 객체들 처리
    //    var sceneTargets = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
    //                                 .OfType<IValidatable>();

    //    foreach (var target in sceneTargets)
    //    {
    //        target.OnManualValidate();
    //        EditorUtility.SetDirty(target as MonoBehaviour);
    //    }

    //    // 2. 프로젝트 내 모든 프리팹 에셋 처리
    //    // "t:Prefab" 필터를 사용해 모든 프리팹의 GUID를 가져옵니다.
    //    string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

    //    foreach (string guid in prefabGuids)
    //    {
    //        string path = AssetDatabase.GUIDToAssetPath(guid);
    //        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);

    //        if (prefabRoot == null) continue;

    //        // 프리팹 내부에서 IValidatable을 가진 모든 컴포넌트 찾기
    //        var components = prefabRoot.GetComponentsInChildren<MonoBehaviour>(true)
    //                                   .OfType<IValidatable>();

    //        foreach (var comp in components)
    //        {
    //            comp.OnManualValidate();
    //            EditorUtility.SetDirty(comp as MonoBehaviour);
    //        }
    //    }

    //    // 최종 변경사항 물리적 저장
    //    AssetDatabase.SaveAssets();
    //    AssetDatabase.Refresh();

    //    Debug.Log($"VALIDATE FINISHED: {(Time.realtimeSinceStartup - startTime):0.#0}s");
    //}
}

#endif