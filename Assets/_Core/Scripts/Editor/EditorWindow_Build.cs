using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Events;
using static EditorWindow_Build;

public partial class EditorWindow_Build : EditorWindow
{
    const string c_projectName = "ThreeKingz";
    const string c_key = "EditorBuild_";
    const string c_addressableProfileName = "ROKSAN_Bundle";

    string m_projectName = "";
    UserData m_userData = new();
    public void SetUserData(UserData _userData) => m_userData = _userData;

    static Queue<string> m_buildLog = new();

    private void OnEnable()
    {
        m_projectName = new DirectoryInfo(Application.dataPath).Parent.Name;

        string userData = EditorPrefs.GetString(c_key + "_userData_" + m_projectName);
        if (userData.IsActive())
            m_userData = JsonUtility.FromJson<UserData>(userData);

        if (EditorPrefs.HasKey(c_key + "_position") == false)
        {
            Rect postion = position;
            postion.width = 400;
            postion.height = 250;
            position = postion;
        }
    }
    private void OnDestroy()
    {
        EditorPrefs.SetString(c_key + "_position", "");
    }

    #region Drawing GUI

    GUIContent m_contentServerType;
    GUIContent m_contentBuildTarget;
    bool m_isAddressableBuild = false;

    private void OnGUI()
    {
        OnGUI_PlayerSetting();

        if (m_userData.buildTarget == BuildTarget.Android)
            OnGUI_AndroidInfo();

        OnGUI_BUttons();
    }


    void OnGUI_PlayerSetting()
    {
        GUIStyle styleBoldLabel = new GUIStyle(GUI.skin.label);
        styleBoldLabel.fontStyle = FontStyle.Bold;

#if SERVICE_DEV
        GUILayout.Label("DEV", styleBoldLabel);
#elif SERVICE_LIVE
        GUILayout.Label("LIVE", styleBoldLabel);
#endif

        GUILayout.Label("Player Setting", styleBoldLabel);

        // Service version check
        {
            AddHorizontalLayout(() =>
            {
                m_userData.serviceType =
                    (ServiceType)EditorGUILayout.EnumPopup(m_contentServerType, m_userData.serviceType);
            }, "Service");
        }

        // build target
        {
            AddHorizontalLayout(() =>
            {
                m_userData.buildTarget =
                    (BuildTarget)EditorGUILayout.EnumPopup(m_contentBuildTarget, m_userData.buildTarget);
            }, "BuildTarget");
        }

        var seasonIndex = AddTextArea("Season", m_userData.seasonIndex.ToString());
        if (seasonIndex != m_userData.seasonIndex.ToString())
            int.TryParse(seasonIndex, out m_userData.seasonIndex);

        m_userData.version = AddTextArea("Version", m_userData.version);

        GUILayout.Space(10);
    }

    void OnGUI_AndroidInfo()
    {
        GUIStyle styleBoldLabel = new GUIStyle(GUI.skin.label);
        styleBoldLabel.fontStyle = FontStyle.Bold;
        GUILayout.Label("Android", styleBoldLabel);

        m_userData.androidData.bundleVersionCode = AddTextArea("Bundle", m_userData.androidData.bundleVersionCode);

        AddHorizontalLayout(() =>
        {
            m_userData.isStore = GUILayout.Toggle(m_userData.isStore, "");
        }, "isStore");

        if (m_userData.isStore == true)
        {
            AddHorizontalLayout(() =>
            {
                m_userData.androidData.isAPK = GUILayout.Toggle(m_userData.androidData.isAPK, "");
            }, "isAPK");
        }
    }

    static bool m_isSuccessBuild = false;
    void OnGUI_BUttons()
    {
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Save Data", GUILayout.Height(20)))
        {
            // 저장만
            Run(false, false);
        }

        if (GUILayout.Button("Build Start", GUILayout.Height(20)))
        {
            // 빌드
            Run(true, false);
        }

        if (m_isAddressableBuild == false)
        {
            if (GUILayout.Button("Build & Addressable", GUILayout.Height(20)))
            {
                m_isAddressableBuild = true;
                //BuildStart(true);
            }
        }
        else
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Build Start", GUILayout.Height(20)))
            {
                // 빌드 and 어드레서블 빌드
                Run(true, true);
                m_isAddressableBuild = false;
            }

            if (GUILayout.Button("Cancel", GUILayout.Height(20)))
            {
                m_isAddressableBuild = false;
            }

            GUILayout.EndHorizontal();
        }

        //if (GUILayout.Button("Reset", GUILayout.Height(20)))
        //    OnButton_Reset();
    }

    public void Run(bool _isBuild, bool _isAddressable)
    {
        switch (m_userData.buildTarget)
        {
            case BuildTarget.Android:
            case BuildTarget.iOS:
            case BuildTarget.WebGL:
                break;
            default:
                Debug.Log("UNSUPPORTED PLATFORM!!");
                return;
        }

        var dtStart = DateTime.Now;
        m_buildLog.Clear();

        // Save Data
        {
            SaveData();

            WriteVersionData();

            PlayerSettings.bundleVersion = m_userData.version;
        }

        if (_isBuild == false)
        {
            EditorUserBuildSettings.buildAppBundle = m_userData.isStore == true && m_userData.androidData.isAPK == false;
            AssetDatabase.SaveAssets();
            return;
        }

        PlayerSettings.productName = c_projectName;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;

        if ((int)m_userData.buildTarget != (int)EditorUserBuildSettings.activeBuildTarget)
            EditorUserBuildSettings.SwitchActiveBuildTarget(GetBuildTargetGroup(), (BuildTarget)(int)m_userData.buildTarget);

        if (_isAddressable == true)
        {
            m_isSuccessBuild = Build_Addressables(true, m_userData.serviceType);
            CopyBundle();
        }
        else
            m_isSuccessBuild = true;

        if (m_isSuccessBuild == true)
        {
            m_isSuccessBuild = false;
            Build_Start();
        }

        m_buildLog.Enqueue($"Build{(_isAddressable ? " & Addressable" : "")}: {m_userData.serviceType}: {(m_isSuccessBuild ? "Success" : "Failed")}: {(DateTime.Now - dtStart)}");
        while (m_buildLog.Count > 0)
            Debug.Log(m_buildLog.Dequeue());
    }

    void AddHorizontalLayout(UnityAction _action, string _title = "", int _leftSpace = 10)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(_leftSpace);

        if (string.IsNullOrEmpty(_title) == false)
            GUILayout.Label(_title, GUILayout.MaxWidth(90));

        _action();
        GUILayout.EndHorizontal();
    }

    string AddTextArea(string _title, string _text, float _height = 20)
    {
        string result = "";
        AddHorizontalLayout(
            () => { result = GUILayout.TextArea(_text, EditorStyles.textArea, GUILayout.MinHeight(_height)); }, _title);
        return result;
    }

    #endregion

    void FileCopyResultFile(string _fileName)
    {
        // 파일 복사
        try
        {
            var copyFrom = string.Format("0_Bin/{0}/{1}/{2}", m_userData.buildTarget, m_userData.serviceType, _fileName);

            // 배치모드가 아니면 NAS에 올리자.
            if (Application.isBatchMode == false)
            {
                var path = @"\\10.10.10.99\web\" + PlayerSettings.productName + @"\" + $"{(m_userData.isStore == false || m_userData.androidData.isAPK ? "apk" : "aab")}" + @"\" + m_userData.serviceType;

                if (m_userData.isStore == true)
                    path += @"\STORE";

                if (Directory.Exists(path) == false)
                    Directory.CreateDirectory(path);

                path += _fileName;

                m_buildLog.Enqueue("Copy File TO NAS: " + path);
                File.Copy(copyFrom, path, true);
            }
        }
        catch (Exception _e)
        {
            m_buildLog.Enqueue("ERROR: CopyFile: " + _e.Message);
        }
    }

    void BuildFinished()
    {
        while (m_buildLog.Count > 0)
            Debug.Log(m_buildLog.Dequeue());

        Debug.Log($"BUILD FINISHED");
    }

    void Build_Start()
    {
        if (GetCheckItem() == false)
            return;

        // 폴더정보
        string filePath = string.Format("0_Bin/{0}/{1}", m_userData.buildTarget, m_userData.serviceType);

        if (Directory.Exists(filePath) == false)
            Directory.CreateDirectory(filePath);

        // PLAYERT SETTING
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
        Enum.TryParse(m_userData.buildTarget.ToString(), out BuildTarget buildTarget);
        options.target = buildTarget;
        options.targetGroup = GetBuildTargetGroup();
        options.options = m_userData.buildTarget == BuildTarget.iOS ? BuildOptions.AcceptExternalModificationsToPlayer : BuildOptions.None;

        string fileName = "";
        UnityAction onComplete = null;
        switch (m_userData.buildTarget)
        {
            case BuildTarget.Android:
                {
                    PlayerSettings.Android.keystorePass = "dhdnrjrkwmdk1!";
                    PlayerSettings.Android.keyaliasPass = "dhdnrjrkwmdk1!";

                    PlayerSettings.Android.bundleVersionCode = int.Parse(m_userData.androidData.bundleVersionCode);

                    EditorUserBuildSettings.buildAppBundle = m_userData.isStore == true && m_userData.androidData.isAPK == false;

                    string key = string.Format("{0}_{1}{2}",
                        c_projectName,
                        GetVersion(),
                        m_userData.isStore == false ? "" : "_STORE");

                    string extention = m_userData.isStore == false || m_userData.androidData.isAPK ? "apk" : "aab";
                    fileName += $"/{key}.{extention}";
                    filePath += fileName;

                    onComplete = () =>
                    {
                        FileCopyResultFile(fileName);
                        //if (m_userData.isStore == false || m_userData.androidData.isAPK)
                        //{
                        //    if (StartBuild(filePath) == true)
                        //    {
                        //        //SaveHistory(key);
                        //        FileCopyResultFile(fileName);

                        //        BuildSuccess();
                        //    }
                        //}
                        //else
                        //{
                        //    var buildOptions = new BuildPlayerOptions
                        //    {
                        //        scenes = EditorBuildSettings.scenes.ToList().Select(x => x.path).ToArray(),
                        //        locationPathName = filePath,
                        //        target = BuildTarget.Android,
                        //        targetGroup = BuildTargetGroup.Android,
                        //        options = BuildOptions.None
                        //    };

                        //    AssetPackConfig assetPackConfig = new AssetPackConfig();

                        //    if (AppBundlePublisher.Build(buildOptions, assetPackConfig, true))
                        //    {
                        //        FileCopyResultFile(fileName);
                        //        BuildSuccess();
                        //    }
                        //}
                    };
                }
                break;
            case BuildTarget.iOS:
                {
                    onComplete = () =>
                    {
                        System.Diagnostics.Process process = new();

                        process.StartInfo.FileName = "open";
                        process.StartInfo.Arguments = filePath;
                        process.Start();
                    };
                }
                break;
            case BuildTarget.WebGL:
                {
                    var folderPath = filePath;
                    filePath += "/web";

                    EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
                    options.subtarget = (int)WebGLTextureSubtarget.DXT;

                    onComplete = () =>
                    {
                        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
                        options.subtarget = (int)WebGLTextureSubtarget.ASTC;
                        options.locationPathName = filePath + "_mobile";

                        BuildReport report_mobile = BuildPipeline.BuildPlayer(options);

                        if (report_mobile.summary.result == BuildResult.Succeeded)
                        {
                            string mobile_directory = filePath + "_mobile/Build";
                            string[] files = Directory.GetFiles(mobile_directory);

                            for (int i = 0; i < files.Length; i++)
                            {
                                File.Copy(
                                    files[i],
                                    $"{filePath}/Build/{Path.GetFileName(files[i])}",
                                    true
                                );
                            }

                            Directory.Delete(options.locationPathName, true);
                            File.Delete($"{filePath}/index.html");
                        }
                        else
                        {
                            m_buildLog.Enqueue($"Build Failed: _MOBILE");
                        }

                        SetWebGLIndexFile_BuildIndex();
                    };
                }
                break;
        }

        options.locationPathName = filePath;

        // BUILD START

        EditorUserBuildSettings.SwitchActiveBuildTarget(options.targetGroup, buildTarget);

        // Build Player!!
        BuildReport report = BuildPipeline.BuildPlayer(options);

        m_isSuccessBuild = report.summary.result == BuildResult.Succeeded;

        if (m_isSuccessBuild)
            onComplete();
    }

    bool Build_Addressables(bool _isNew, ServiceType _serviceType)
    {
        string build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
        string settings_asset = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";

        if (_isNew)
            BuildCache.PurgeCache(false);

        AddressableAssetSettings settings =
            AssetDatabase.LoadAssetAtPath<ScriptableObject>(settings_asset) as AddressableAssetSettings;

        if (settings == null)
        {
            m_buildLog.Enqueue("Addressable Setting is null");
            return false;
        }

        string profileId = settings.profileSettings.GetProfileId(c_addressableProfileName);
        settings.activeProfileId = profileId;

        IDataBuilder builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder;

        if (builderScript == null)
        {
            m_buildLog.Enqueue(build_script + " couldn't be found or isn't a build script.");
            return false;
        }

        string remoteBuildPath = $"Bundle/{_serviceType}/[BuildTarget]/";

        int index = settings.DataBuilders.IndexOf((ScriptableObject)builderScript);
        settings.ActivePlayerDataBuilderIndex = index;
        settings.profileSettings.SetValue(profileId, "Remote.BuildPath", remoteBuildPath);
        EditorUtility.SetDirty(settings);

        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

        //기본은 Editor 로 해놓자..
        remoteBuildPath = "Bundle/_Last/[BuildTarget]/";
        settings.profileSettings.SetValue(profileId, "Remote.BuildPath", remoteBuildPath);

        if (result.Error.IsActive())
        {
            m_buildLog.Enqueue("Addressables build error encountered: " + result.Error);
            return false;
        }
        else
            m_buildLog.Enqueue("Addressables build successed: " + remoteBuildPath);

        return true;
    }

    [MenuItem("Rev9/Build/CopyBundle")]
    static void CB()
    {
        string remoteBuildPath = $"Bundle/Editor/WebGL/";
        if (Directory.Exists(remoteBuildPath))
        {
            string[] files = Directory.GetFiles(remoteBuildPath);

            string copyPath = $"Bundle/_Last/WebGL/";

            if (Directory.Exists(copyPath) == false)
                Directory.CreateDirectory(copyPath);

            foreach (var file in files)
            {
                using (FileStream originalFileStream = File.OpenRead(file))
                using (FileStream compressedFileStream = File.Create(copyPath + Path.GetFileName(file)))
                using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                {
                    originalFileStream.CopyTo(compressionStream);
                }
            }
        }
    }

    void CopyBundle()
    {
        string remoteBuildPath = $"Bundle/{m_userData.serviceType}/{m_userData.buildTarget}/";
        if (Directory.Exists(remoteBuildPath))
        {
            string[] files = Directory.GetFiles(remoteBuildPath);

            string copyPath = $"Bundle/_Last/{m_userData.buildTarget}/";

            if (Directory.Exists(copyPath) == false)
                Directory.CreateDirectory(copyPath);

            foreach (var file in files)
            {
                using (FileStream originalFileStream = File.OpenRead(file))
                using (FileStream compressedFileStream = File.Create(copyPath + Path.GetFileName(file)))
                using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                {
                    originalFileStream.CopyTo(compressionStream);
                }
            }
        }
    }

    string GetVersion()
    {
        var value = Application.version.Split('.');
        var version = string.Join(".", value.Take(2));
        if (value.Length > 2)
            version += $"_{string.Join(".", value.Skip(2))}";

        return $"{m_userData.seasonIndex}.{version}";
    }

    bool GetCheckItem()
    {
        try
        {
            string[] arrVersion = m_userData.version.Split('.');
            for (int i = 0; i < arrVersion.Length; i++)
                int.Parse(arrVersion[i]);
        }
        catch
        {
            m_buildLog.Enqueue("GetCheckItem: FAILED");
            return false;
        }

        return true;
    }

    void SaveData()
    {
        if (GetCheckItem() == false)
            return;

        EditorPrefs.SetString(c_key + "_userData_" + m_projectName, JsonUtility.ToJson(m_userData));
        PlayerSettings.bundleVersion = $"{m_userData.version}";

#if UNITY_WEBGL
        PlayerSettings.WebGL.exceptionSupport = m_userData.serviceType == ServiceType.Live ?
            WebGLExceptionSupport.None : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
#endif

        // Define
        var buildTargetGroup = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(GetBuildTargetGroup());
        string symbols = PlayerSettings.GetScriptingDefineSymbols(buildTargetGroup);

        // Service
        {
            ServiceType i = 0;
            string curSymbol = $"SERVICE_{m_userData.serviceType.ToString().ToUpper()}";
            for (; i <= ServiceType.Live; i++)
            {
                string symbol = $"SERVICE_{i.ToString().ToUpper()}";
                if (symbols.Contains(symbol) == true)
                {
                    if (i != m_userData.serviceType)
                        symbols = symbols.Replace(symbol, curSymbol);
                    break;
                }
            }

            if (i > ServiceType.Live)
                symbols += $"{(symbols.Length > 0 ? ";" : "")}{curSymbol}";
        }

        // STORE VERSION
        if (m_userData.buildTarget == BuildTarget.Android ||
            m_userData.buildTarget == BuildTarget.iOS)
        {
            // ios 는 무조건 스토어 버전임
            if (m_userData.isStore || m_userData.buildTarget == BuildTarget.iOS)
            {
                if (symbols.Contains("STORE_VERSION_NONE") == true)
                    symbols = symbols.Replace("STORE_VERSION_NONE", "STORE_VERSION");
                else if (symbols.Contains("STORE_VERSION") == false)
                    symbols += ";STORE_VERSION";
            }
            else if (symbols.Contains("STORE_VERSION_NONE") == false)
            {
                if (symbols.Contains("STORE_VERSION"))
                    symbols = symbols.Replace("STORE_VERSION", "STORE_VERSION_NONE");
                else
                    symbols += ";STORE_VERSION_NONE";
            }
        }

        PlayerSettings.SetScriptingDefineSymbols(buildTargetGroup, symbols);
    }

    BuildTargetGroup GetBuildTargetGroup()
    {
        if (m_userData.buildTarget == BuildTarget.Android)
            return BuildTargetGroup.Android;
        else if (m_userData.buildTarget == BuildTarget.iOS)
            return BuildTargetGroup.iOS;
        else if (m_userData.buildTarget == BuildTarget.WebGL)
            return BuildTargetGroup.WebGL;

        return BuildTargetGroup.Unknown;
    }

    void WriteVersionData()
    {
        string filePath = "Assets/Resources/EditorData";

        if (Directory.Exists(filePath) == false)
            Directory.CreateDirectory(filePath);

        filePath += $"/BuildData.json";

        Dictionary<string, int> buildData = new()
        {
            {"seansonIndex",  m_userData.seasonIndex}
        };

        string jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(buildData);
        File.WriteAllText(filePath, jsonText);
    }

    void SetWebGLIndexFile_BuildIndex()
    {
        string path = Application.dataPath + $"/../0_Bin/WebGL/{m_userData.serviceType}/index.html";
        try
        {
            string html = File.ReadAllText(path);


            html = Regex.Replace(html,
                @"let version_BuildIndex = [^;]+;",
                $"let version_BuildIndex = \"{GetWebGLBuildIndex()}\";");

            File.WriteAllText(path, html);
        }
        catch
        {
        }
    }

    string GetWebGLBuildIndex()
    {
        if (Application.isBatchMode)
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-buildNumber" && i + 1 < args.Length)
                    return args[i + 1];
            }
        }
        else
        {
            var value = Application.version.Split('.');

            var version = $"{string.Join(".", value.Skip(1))}";
            version += $"_{value[2]}";

            return version;
        }
        return null;
    }

    public class UserData
    {
        public ServiceType serviceType = ServiceType.Dev;
        public BuildTarget buildTarget = BuildTarget.Android;
        public AndroidSettingData androidData = new();

        public string version = "0.1.1";
        public int seasonIndex = 0;

        public bool isStore = true;
    }

    [Serializable]
    public class AndroidSettingData
    {
        public bool isAPK = true;
        public string bundleVersionCode;
    }
}

public enum PlatformType
{
    Android = 1,
    IOS,
    APK,
    WebGL,
}

public enum ServiceType
{
    Dev,
    Staging,
    Live,
}
