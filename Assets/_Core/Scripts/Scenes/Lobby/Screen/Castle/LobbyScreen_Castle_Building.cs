using UnityEngine;

public class LobbyScreen_Castle_Building : MonoBehaviour, IValidatable
{
    [SerializeField]
    CastleObjectType m_objectType;
    public CastleObjectType objectType => m_objectType;

    public void StartUpgrade()
    {
        m_element.anim.Play($"Castle_BuildFX_{m_objectType}_Act");
    }

    public void FinishUpgrade()
    {
        m_element.anim.Play($"Castle_BuildFX_{m_objectType}_End");
    }


    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Animator anim;

        public void Initialize(Transform _transform)
        {
            anim = _transform.GetComponent<Animator>();
        }
    }
    #endregion VALIDATE

}
