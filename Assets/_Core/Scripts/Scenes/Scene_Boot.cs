using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
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

#if UNITY_EDITOR
        Configure.instance.SetPC(true);
#elif UNITY_WEBGL
        MessageHandler.StartGame();
        MessageHandler.UnityProgressCall(1, 1);
        Configure.instance.SetPC(MessageHandler.IsMobileBrowser() == false);
#else
        Configure.instance.SetPC(false);
#endif

        await m_element.logo.DOFade(1, 0.5f).AsyncWaitForCompletion();
        var timeStart = Time.realtimeSinceStartup;

        // 사이에 세팅할것들
#if SERVICE_DEV && !UNITY_EDITOR
        {
            // 개발 도중 구조가 바뀌는것땜에 에러가 나는 경우가 있어서. 그거 대응
            var assetBuild = Resources.Load<TextAsset>("EditorData/BuildData");

            if (assetBuild != null)
            {
                string key = "pp_build_data";

                var build = Newtonsoft.Json.Linq.JObject.Parse(assetBuild.ToString());
                long tickData = (long)build["dt_build"];

                if (PPWorker.HasKey(key))
                {
                    long tickLocal = long.Parse(PPWorker.Get<string>(key));

                    if (tickLocal != tickData)
                    {
                        //옵션은 남겨두자
                        var optionData = PPWorker.Get<Data_Option.OptionData>(PlayerPrefsType.OPTION);

                        PlayerPrefs.DeleteAll();
                        PPWorker.Set(key, tickData);

                        PPWorker.Set(PlayerPrefsType.OPTION, optionData);
                    }
                }
                else
                {
                    //옵션은 남겨두자
                    var optionData = PPWorker.Get<Data_Option.OptionData>(PlayerPrefsType.OPTION);

                    PlayerPrefs.DeleteAll();
                    PPWorker.Set(key, tickData);

                    PPWorker.Set(PlayerPrefsType.OPTION, optionData);
                }
            }
        }
#endif
        await UniTask.WhenAll(tasks.ToArray());
#if !UNITY_EDITOR
        IngameLog.Add($"Boot: StartAsync: Finished: {(Time.realtimeSinceStartup - timeStart):0.#0}s");
#endif

        var time = Time.realtimeSinceStartup - timeStart;
        if (time < 1)
            await UniTask.WaitForSeconds(1 - time);

#if !UNITY_EDITOR
        IngameLog.Add($"BOOTS: {time:0.##0}s");
#endif

        await m_element.logo.DOFade(0, 0.5f).AsyncWaitForCompletion();

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
