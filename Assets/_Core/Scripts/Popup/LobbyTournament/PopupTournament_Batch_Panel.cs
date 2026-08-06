using UnityEngine;
using Rev9.Tournament;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class PopupTournament_Batch_Panel : MonoBehaviour, IValidatable
{
    Dictionary<string, GameObject> m_db = new();

    private void Awake()
    {
        foreach (var slot in m_element.slots)
        {
            for (int i = 0; i < slot.childCount; i++)
                Destroy(slot.GetChild(i).gameObject);
        }
    }

    private void Start()
    {
    }

    public async UniTask SetBatchDataAsync(TournamentBatchData _batchData)
    {
        foreach (var hero in m_db)
            hero.Value.gameObject.SetActive(false);

        for (int i = 0; i < _batchData.skinKey.Length; i++)
        {
            var key = _batchData.skinKey[i];
            var slot = m_element.slots[_batchData.position[i]];

            GameObject hero = null;
            if (m_db.ContainsKey(key))
            {
                hero = m_db[key];
                hero.transform.SetParent(slot);
                hero.gameObject.SetActive(true);
            }
            else
            {
                var newhero = Instantiate(await AddressableManager.instance.GetHeroCharacterAsync(key), slot).GetComponent<CharacterComponent>();
                newhero.transform.localScale = Vector2.one * 70;
                newhero.Awake();
                newhero.move.SetFlip(true);
                hero = newhero.gameObject;

                m_db.Add(key, hero);
            }

            hero.transform.localPosition = Vector3.zero;
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Transform[] slots;

        public void Initialize(Transform _transform)
        {
            slots = new Transform[_transform.childCount];
            for (int i = 0; i < _transform.childCount; i++)
                slots[i] = _transform.GetChild(i);
        }
    }
    #endregion VALIDATE

}
