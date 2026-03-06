using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Scene_Boot : MonoBehaviour, IValidatable
{
    private void Awake()
    {
        MessageHandler.instance.Create();
#if UNITY_EDITOR
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        type.GetMethod("Clear").Invoke(new object(), null);
#endif

        Configure.instance.isBooted = true;
    }

    private void Start()
    {
        StartAsync().Forget();
    }

    async UniTask StartAsync()
    {
        List<UniTask> tasks = new();
        tasks.Add(AddressableManager.instance.InitializeAsync());
        tasks.Add(TableManager.instance.InitializeAsync());

        var color = m_element.logo.color;
        color.a = 0;
        m_element.logo.color = color;

        await UniTask.WaitForEndOfFrame();

        await m_element.logo.DOFade(1, 0.5f).AsyncWaitForCompletion();

        float timeStart = Time.time;

        await UniTask.WhenAll(tasks);

        var time = Time.time - timeStart;
        if (time < 1)
            await UniTask.WaitForSeconds(1 - time);

        await m_element.logo.DOFade(0, 0.5f).AsyncWaitForCompletion();

#if !UNITY_EDITOR && UNITY_WEBGL
        MessageHandler.StartGame();
        MessageHandler.UnityProgressCall(1, 1);
#endif

        AddressableManager.instance.LoadScene("01_Login");
    }

    public void OnManualValidate()
    {
        m_element.Initalize(transform);
    }

    [SerializeField]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        [SerializeField]
        SpriteRenderer m_logo;
        public SpriteRenderer logo => m_logo;

        public void Initalize(Transform _transform)
        {
            m_logo = _transform.GetComponent<SpriteRenderer>("Logo");
        }
    }
}
