using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rev9.Edit.CustomCharacter
{
    public class EditWorker_CustomHero : BaseWorker<EditWorker_CustomHero>
    {
        private const string c_path = "AddressableDatas/Prefab/Hero_Custom";

        public List<string> pathPrefabs = new();

        public void Initialize(Transform _parts)
        {
            pathPrefabs = GetHeroPrefabPaths();
        }

        public static List<string> GetHeroPrefabPaths()
        {
            List<string> prefabPaths = new List<string>();

            // 폴더 존재 유무 확인
            string fullPath = Path.Combine(Application.dataPath, c_path);

            // t:Prefab 필터를 사용해 해당 폴더 및 하위 폴더의 프리팹 탐색
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { c_path });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                prefabPaths.Add(path);
            }

            return prefabPaths;
        }

        // 2. 특정 경로의 프리팹을 인스턴티에이트
        public static GameObject SpawnHeroPrefab(string _path)
        {
            // 에셋 로드
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(_path);

            if (prefabAsset == null)
            {
                Debug.LogError($"[HeroCustomPrefabLoader] 프리팹을 로드할 수 없습니다: {_path}");
                return null;
            }

            // 씬에 생성
            GameObject spawnedObject = Object.Instantiate(prefabAsset);
            spawnedObject.name = prefabAsset.name;

            return spawnedObject;
        }

    }
}