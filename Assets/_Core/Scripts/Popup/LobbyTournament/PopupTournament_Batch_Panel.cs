using UnityEngine;
using UnityEngine.UI;
using Rev9.Tournament;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class PopupTournament_Batch_Panel : MonoBehaviour, IValidatable
{
    Dictionary<string, GameObject> m_db = new();

    public RectTransform GetSlot(int _slotIndex)
        => (RectTransform)m_element.slots[_slotIndex].transform;

    public CharacterComponent GetCharacter(int _slotIndex)
    {
        var slot = m_element.slots[_slotIndex];
        return slot.childCount == 0 ? null : slot.GetChild(0).GetComponent<CharacterComponent>();
    }

    private void Awake()
    {
        foreach (var slot in m_element.slots)
        {
            for (int i = 0; i < slot.childCount; i++)
                Destroy(slot.GetChild(i).gameObject);

            int idxSibling = slot.GetSiblingIndex();
            var button = slot.GetComponent<Button>();
            button.interactable = slot.GetComponent<Image>().enabled = false;
            button.onClick.AddListener(() => OnButtonAsync(idxSibling).Forget());
        }
    }

    bool m_isOpenInfo = false;
    public async UniTask OnButtonAsync(int _idx)
    {
        var slot = m_element.slots[_idx];
        if (slot.childCount == 0 || m_isOpenInfo == true)
            return;

        m_isOpenInfo = true;

        var hero = slot.GetChild(0).GetComponent<CharacterComponent>();
        await PopupManager.instance.OpenPopupAndWait(PopupType.Hero_HeroInfo, hero.info);

        m_isOpenInfo = false;
    }

    private void Start()
    {
    }

    public async UniTask SetBatchDataAsync(TournamentBatchData _batchData)
    {
        foreach (var hero in m_db)
        {
            if (hero.Value.gameObject.activeSelf == true)
            {
                var slot = hero.Value.transform.parent;
                slot.GetComponent<Image>().enabled = slot.GetComponent<Button>().interactable = false;
                hero.Value.gameObject.SetActive(false);
                hero.Value.transform.SetParent(transform);
            }
        }

        for (int i = 0; i < _batchData.heroes.Count; i++)
        {
            var heroData = _batchData.heroes[i];

            var slot = m_element.slots[heroData.sortIdx];
            slot.GetComponent<Button>().interactable = slot.GetComponent<Image>().enabled = true;
            var skinKey = heroData.skin;

            GameObject hero = null;
            if (m_db.ContainsKey(skinKey))
            {
                hero = m_db[skinKey];
                hero.transform.SetParent(slot);
                hero.gameObject.SetActive(true);
            }
            else
            {
                var newhero = Instantiate(await AddressableManager.instance.GetHeroCharacterAsync(skinKey), slot).GetComponent<CharacterComponent>();
                newhero.transform.localScale = Vector2.one * 70;
                newhero.Awake();
                newhero.SetInfo(heroData);
                newhero.DeleteCollider();
                newhero.move.SetFlip(true);
                hero = newhero.gameObject;

                m_db.Add(skinKey, hero);
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
