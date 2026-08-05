using UnityEngine;
using Rev9.Tournament;

public class PopupTournament_Batch_Panel : MonoBehaviour, IValidatable
{
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
        var heroes = TournamentWorker.instance.GetHeroes(true);
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
