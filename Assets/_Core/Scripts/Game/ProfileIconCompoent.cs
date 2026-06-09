using Cysharp.Threading.Tasks;
using UnityEngine;

public class ProfileIconCompoent : MonoBehaviour, IValidatable
{
    public async UniTask SetProfileDataAsync(string _skin)
    {
        for (int i = 0; i < m_element.panel.childCount; i++)
            Destroy(m_element.panel.GetChild(i).gameObject);

        var prefab = await AddressableManager.instance.GetHeroIconAsync(_skin);

        if (prefab != null)
        {
            var icon = Instantiate(prefab, m_element.panel);

            var rtParent = icon.transform.parent as RectTransform;
            await UniTask.WaitUntil(() => rtParent.rect.width > 0 || rtParent.rect.height > 0);

            icon.AutoResizeParent().name = _skin;
        }
    }

    public void SetProfileData(int _idxProfile) { }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Transform panel;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Panel");
        }
    }
    #endregion VALIDATE

}
