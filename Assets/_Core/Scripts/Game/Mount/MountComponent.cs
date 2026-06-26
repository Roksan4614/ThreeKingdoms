using UnityEngine;

public class MountComponent : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        m_element.objMount.SetActive(false);

        // 팝업에서 등장할때만 켜주게ㅎㅎ
        m_element.fxRecall.gameObject.SetActive(BossRaidWorker.instance.isRunning == false);
    }

    public void SetMount(CharacterComponent _character, bool _isMount)
    {
        var parts = _character.element.parts;

        // 무기를 일단 바꿔보자
        WeaponChange(parts, _isMount);

        parts.SetParent(_isMount ? m_element.slot : _character.element.panel);
        parts.localPosition = Vector3.zero;

        m_element.objMount.SetActive(_isMount);

        m_element.fxRecall.gameObject.SetActive(true);
        m_element.fxRecall.Simulate(0);
        m_element.fxRecall.Play();

        if (_isMount == false)
        {
            _character.anim.Play(CharacterAnimType.Idle);
        }
    }

    void WeaponChange(Transform _parts, bool _isMount)
    {
        var weapon = _parts.Find("Weapon");

        if (_isMount)
        {
            int idxMount = -1;
            for (int i = 0; i < weapon.childCount; i++)
            {
                if (weapon.GetChild(i).name.Contains("Mount", System.StringComparison.OrdinalIgnoreCase))
                {
                    idxMount = i;
                    weapon.GetChild(i).gameObject.SetActive(true);
                }
                else
                    weapon.GetChild(i).gameObject.SetActive(false);
            }
            if (idxMount == -1)
                weapon.GetChild(0).gameObject.SetActive(false);
        }
        else
        {
            for (int i = 0; i < weapon.childCount; i++)
                weapon.GetChild(i).gameObject.SetActive(i == 0);
        }
    }

    public void Play(string _key)
    {
        m_element.animator.CrossFade(_key, 0);
    }


    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Animator animator;
        public Transform slot;

        public ParticleSystem fxRecall;

        public GameObject objMount => animator.transform.parent.parent.gameObject;

        public void Initialize(Transform _transform)
        {
            animator = _transform.GetComponent<Animator>("Mount/Panel/Parts");
            slot = _transform.Find("Mount/Panel/Parts/CharacterSlot");

            fxRecall = _transform.GetComponent<ParticleSystem>("FX_Recall");
        }
    }
    #endregion VALIDATE

}
