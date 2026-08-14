using UnityEngine;

public class TournamentPositionManager : Singleton<TournamentPositionManager>, IValidatable
{

    public Vector3 GetPosition(bool _isMe, int _index)
    {
        return (_isMe ? m_element.me : m_element.other).GetChild(_index).position;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Transform me;
        public Transform other;
        public void Initialize(Transform _transform)
        {
            me = _transform.Find("Me");
            other = _transform.Find("Other");
        }
    }
    #endregion VALIDATE

}
