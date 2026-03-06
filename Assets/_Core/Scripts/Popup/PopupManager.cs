using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum PopupType
{
    NONE = -1,

    Hero_Filter,
    Hero_Sort,
    Hero_HeroInfo,

    Modal_Start,

    MAX
}

public class PopupManager : MonoSingleton<PopupManager>, IValidatable
{
    private Dictionary<PopupType, AsyncOperationHandle<GameObject>> m_dicPopup = new();

    protected override void OnAwake()
    {
        transform.SetSiblingIndex(0);
    }

    protected override void OnDestroy()
    {
        foreach (var h in m_dicPopup)
            h.Value.Release();

        base.OnDestroy();
    }

    public async UniTask<GameObject> LoadAsset(PopupType _popupType)
    {
        if (m_dicPopup.ContainsKey(_popupType))
            return m_dicPopup[_popupType].Result;

        await AddressableManager.instance.LoadAssetAsync<GameObject>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_dicPopup.ContainsKey(_popupType) == false)
                    m_dicPopup.Add(_popupType, data.Value);
            }
        }, null, $"Popup/{_popupType}.prefab");

        return m_dicPopup.ContainsKey(_popupType) ? m_dicPopup[_popupType].Result : null;
    }

    public async UniTask<GameObject> OpenPopup(PopupType _popupType, params object[] _data)
    {
        return (await OpenPopup<BasePopupComponent>(_popupType, _data)).gameObject;
    }

    public async UniTask<T> OpenPopup<T>(PopupType _popupType, params object[] _data) where T : BasePopupComponent
    {
        GameObject popupObject = await LoadAsset(_popupType);

        if (popupObject == null)
            return null;

        string parent = _popupType > PopupType.Modal_Start ? "Modal" : "Popup";

        var popup = Instantiate(popupObject, transform.Find(parent)).GetComponent<BasePopupComponent>();
        popup.name = _popupType.ToString();

        popup.OpenPopup(_data);

        return popup?.GetComponent<T>();
    }

    public IEnumerator DoOpenPopup(PopupType _popupType, params object[] _data)
    {
        yield return OpenPopupAndWait(_popupType, _data).ToCoroutine();
    }

    public async UniTask OpenPopupAndWait(PopupType _popupType, params object[] _data)
    {
        await OpenPopupAndWait<BasePopupComponent>(_popupType, _data);
    }

    public async UniTask<T> OpenPopupAndWait<T>(PopupType _popupType, params object[] _data) where T : BasePopupComponent
    {
        var popup = await OpenPopup<T>(_popupType, _data);

        await UniTask.WaitUntil(() => popup == null || popup.gameObject.activeSelf == false, cancellationToken: destroyCancellationToken)
            .SuppressCancellationThrow();

        return popup;
    }

    public void ShowDimm(bool _isShow, bool _isFade = true)
    {
        ShowDimmAsync(_isShow, _isFade).Forget();
    }

    public async UniTask ShowDimmAsync(bool _isShow, bool _isFade = true)
    {
        if (_isFade)
        {
            if (_isShow)
                m_element.cgMaxDimm.gameObject.SetActive(true);

            await m_element.cgMaxDimm.DOFade(_isShow ? 1f : 0f, 0.5f).AsyncWaitForCompletion();
        }

        m_element.cgMaxDimm.gameObject.SetActive(_isShow);
    }

    public void SetCanvasCamera() => m_element.canvas.worldCamera = CameraManager.instance.main;

    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        [SerializeField]
        Canvas m_canvas;
        public Canvas canvas => m_canvas;

        [SerializeField]
        CanvasGroup m_cgMaxDimm;
        public CanvasGroup cgMaxDimm => m_cgMaxDimm;

        public void Initialize(Transform _transform)
        {
            m_canvas = _transform.GetComponent<Canvas>();
            m_cgMaxDimm = _transform.GetComponent<CanvasGroup>("MAX_Dimm");
        }
    }
}
