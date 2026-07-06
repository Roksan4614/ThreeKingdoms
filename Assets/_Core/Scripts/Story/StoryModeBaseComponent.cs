using UnityEngine;

public class StoryModeBaseComponent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        TeamManager.instance.SetHeroInfoHide(true, false);
        HeroNavigationComponent.instance.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
