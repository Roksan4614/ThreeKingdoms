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

//[SerializeField, HideInInspector]
[SerializeField]
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

[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class ValidateWorkerButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        IValidatable generator = target as IValidatable;

        if (HasValidatable() == false)
            return;

        if (GUILayout.Button("Validate"))
        {
            float startTime = Time.realtimeSinceStartup;

            foreach (var t in targets)
            {
                GameObject target = null;
                if (t is Component component)
                    target = component.gameObject;
                else if (t is GameObject go)
                    target = go;

                if (target == null)
                    continue;

                var validatables = target.GetComponentsInChildren<IValidatable>(includeInactive: true);

                foreach (var v in validatables)
                {
                    v.OnManualValidate();

                    if (v is Object unityObject)
                    {
                        EditorUtility.SetDirty(unityObject);
                    }
                }
            }
            AssetDatabase.SaveAssets();

            Debug.Log($"VALIDATE FINISHED: {(Time.realtimeSinceStartup - startTime):0.#0}s");
        }
    }

    private bool HasValidatable()
    {
        foreach (var t in targets)
        {
            if (t is Component comp)
            {
                if (comp.GetComponent<IValidatable>() != null)
                    return true;
            }
            else if (t is GameObject go)
            {
                if (go.GetComponent<IValidatable>() != null)
                    return true;
            }
        }
        return false;
    }
}

public static class ValidateWorker
{
    [MenuItem("Rev9/Validate RUN")]
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

    //[MenuItem("Rev9/Validate RUN2")]
    static void Run2()
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
                                         .OfType<IValidatable>();

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
}

#endif