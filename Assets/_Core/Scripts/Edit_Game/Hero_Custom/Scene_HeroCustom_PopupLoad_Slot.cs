using Rev9.Edit.CustomCharacter;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Scene_HeroCustom_PopupLoad_Slot : MonoBehaviour
{
    public UnityAction<string, Transform> actionParts { get; set; }

    public Transform m_parts;

    private void Start()
    {
        transform.GetComponent<Button>().onClick.AddListener(() => actionParts(name, m_parts));

        transform.GetComponent<Button>("btn_delete").onClick.AddListener(() =>
        {
            EditWorker_CustomHero.instance.DeleteFile(gameObject.name);
            Destroy(gameObject);
        });
    }

    public void SetData(string _fileName)
    {
        gameObject.SetActive(true);
        transform.SetText("Name/Text", _fileName);

        var panel = transform.Find("Character/Panel");
        for (int i = 0; i < panel.childCount; i++)
            Destroy(panel.GetChild(i).gameObject);

        var objCharacter = EditWorker_CustomHero.instance.SpawnHeroPrefab(_fileName);
        var trns = objCharacter.transform;
        trns.SetParent(panel);
        trns.localPosition = Vector3.zero;
        trns.localScale = Vector3.one;

        name = _fileName;
        m_parts = objCharacter.transform.Find("Character/Panel/Parts");
    }
}
