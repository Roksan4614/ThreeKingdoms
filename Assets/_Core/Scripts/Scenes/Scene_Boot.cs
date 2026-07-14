using Cysharp.Threading.Tasks;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
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

    const string c_keyVC = "pp_version_code";

    async UniTask StartAsync()
    {
#if UNITY_WEBGL && SERVICE_DEV && !UNITY_EDITOR
        var vc = PPWorker.Get<string>(c_keyVC);

        if (vc.IsActive() == false || vc != Application.version)
        {
            PlayerPrefs.DeleteAll();
            PPWorker.Set(c_keyVC, Application.version);
        }
#endif

        List<UniTask> tasks = new();
        tasks.Add(AddressableManager.instance.InitializeAsync());
        tasks.Add(TableManager.instance.InitializeAsync());

        var color = m_element.logo.color;
        color.a = 0;
        m_element.logo.color = color;

        await UniTask.WaitForEndOfFrame();

        await m_element.logo.DOFade(1, 0.5f).AsyncWaitForCompletion();

        float timeStart = Time.time;

        // 사이에 세팅할것들
#if !UNITY_EDITOR
        {
            // 개발 도중 구조가 바뀌는것땜에 에러가 나는 경우가 있어서. 그거 대응
            //var assetBuild = Resources.Load<TextAsset>("EditorData/BuildData");

            //if (assetBuild != null)
            //{
            //    string key = "START_TIME";

            //    if (PPWorker.HasKey(key))
            //    {
            //        DateTime dtStart = new DateTime(long.Parse(PPWorker.Get<string>(key)));

            //        var build = JObject.Parse(assetBuild.ToString());
            //        DateTime dtBuild = new DateTime((long)build["dt_build"]);

            //        if (dtBuild > dtStart)
            //            PlayerPrefs.DeleteAll();
            //    }
            //    else
            //        PlayerPrefs.DeleteAll();

            //    PPWorker.Set(key, DateTime.UtcNow.Ticks.ToString());
            //}
        }
#endif

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
