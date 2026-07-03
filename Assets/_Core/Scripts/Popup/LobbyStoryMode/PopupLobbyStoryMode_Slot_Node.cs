using TMPro;
using UnityEngine;

public class PopupLobbyStoryMode_Slot_Node : MonoBehaviour, IValidatable
{
    public void SetStoryNode(Table_StoryMode_Node.TableStoryModeNodeData _data)
    {
        m_element.button.text = TableManager.storyString.GetString($"{_data.node_key.ToUpper()}_TITLE");
        m_element.txtChoice.gameObject.SetActive(false);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper button;

        public TextMeshProUGUI txtChoice;

        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<ButtonHelper>("Panel");

            txtChoice = _transform.GetComponent<TextMeshProUGUI>("txt_choice");
        }
    }
    #endregion VALIDATE

}
