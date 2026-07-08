using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupLobbyStoryMode_Slot : MonoBehaviour, IValidatable
{
    public bool SetNodeData(List<List<Table_StoryMode_Node.TableStoryModeNodeData>> _db)
    {
        m_element.txtYear.text = _db[0][0].year + "년";

        var idx = transform.Find("Node").GetSiblingIndex();
        bool isCanceled = false;

        for (int i = 0; i < _db.Count; i++, idx++)
        {
            var node = (idx == transform.childCount ? Instantiate(transform.GetChild(idx - 1), transform) : transform.GetChild(idx))
                .GetComponent<PopupLobbyStoryMode_Slot_Node>();

            node.SetStoryNode(_db[i]);
            node.gameObject.SetActive(true);

            if (node.isOpenNode == true)
                // - 2 한 이유는 위에 두개 node 외가 있어서
                DataManager.storyMode.SetPopupSiblingIndex(transform.GetSiblingIndex(), node.transform.GetSiblingIndex() - 2);

            if (_db[i][0].order_num >= DataManager.storyMode.nextOpenOrderNumber)
            {
                isCanceled = true;
                idx++;
                break;
            }
        }

        for (; idx < transform.childCount; idx++)
            transform.GetChild(idx).gameObject.SetActive(false);

        transform.ForceRebuildLayout();

        return isCanceled == false;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtYear;

        public void Initialize(Transform _transform)
        {
            txtYear = _transform.GetComponent<TextMeshProUGUI>("txt_year");
        }
    }
    #endregion VALIDATE

}
