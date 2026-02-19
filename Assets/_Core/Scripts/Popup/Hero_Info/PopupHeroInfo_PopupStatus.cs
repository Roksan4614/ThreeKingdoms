using UnityEngine;
using UnityEngine.UI;

public class PopupHeroInfo_PopupStatus : MonoBehaviour
{
    private void Start()
    {
        transform.GetComponent<Button>("Dimm").onClick.AddListener(OnClose);
    }

    void OnClose()
    {
        gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }
}
