using Cysharp.Threading.Tasks;
using UnityEngine;

public class Banner_Story : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        m_element.button.onClick.AddListener(() => OnButtonAsync_OpenPopup().Forget());
    }

    async UniTask OnButtonAsync_OpenPopup()
    {
        m_element.button.interactable = false;
        await PopupManager.instance.OpenPopupAndWait(PopupType.LobbyStoryMode);
        m_element.button.interactable = true;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper button;
        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<ButtonHelper>();
        }
    }
    #endregion VALIDATE

}
