using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Rev9.Edit.CustomCharacter
{
    public class EditWorker_CustomHero : BaseWorker<EditWorker_CustomHero>
    {
        private const string c_path = "Assets/AddressableDatas/Prefab/Hero_Custom";

        public List<string> pathPrefabs { get; set; }

        public List<string> GetHeroPrefabPaths()
        {
            pathPrefabs = new();
#if UNITY_EDITOR
            // t:Prefab 필터를 사용해 해당 폴더 및 하위 폴더의 프리팹 탐색
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { c_path });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                pathPrefabs.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }
#endif

            return pathPrefabs;
        }

        public GameObject SpawnHeroPrefab(string _fileName)
        {
            var path = $"{c_path}/{_fileName}.prefab";
            // 에셋 로드
            GameObject prefabAsset = null;
#if UNITY_EDITOR
            AssetDatabase.LoadAssetAtPath<GameObject>(path);
#endif
            if (prefabAsset == null)
            {
                Debug.LogError($"[HeroCustomPrefabLoader] 프리팹을 로드할 수 없습니다: {_fileName}");
                return null;
            }

            // 씬에 생성
            GameObject spawnedObject = Object.Instantiate(prefabAsset);
            spawnedObject.name = prefabAsset.name;
            return spawnedObject;
        }

        public bool HasFileAleady(string _fileName)
        {
#if UNITY_EDITOR
            var path = $"{c_path}/{_fileName}.prefab";
            return AssetDatabase.LoadMainAssetAtPath(path) != null;
#else
return false;
#endif
        }

        public bool SavePrefab(GameObject _sourceObject, string _fileName)
        {
            //string fullDirectoryPath = Path.Combine(Application.dataPath, c_path.Replace("Assets/", ""));
            //if (!Directory.Exists(fullDirectoryPath))
            //{
            //    Directory.CreateDirectory(fullDirectoryPath);
            //    AssetDatabase.Refresh();
            //}

            var path = $"{c_path}/{_fileName}.prefab";
#if UNITY_EDITOR
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(_sourceObject, path, out bool isSuccess);
            return isSuccess;
#else
            return true;
#endif
        }

        public void DeleteFile(string _fileName)
        {
            var path = $"{c_path}/{_fileName}.prefab";
#if UNITY_EDITOR
            AssetDatabase.DeleteAsset(path);
#endif
        }
    }
}