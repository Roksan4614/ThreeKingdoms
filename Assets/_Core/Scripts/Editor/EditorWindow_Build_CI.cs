using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public partial class EditorWindow_Build
{
    private bool m_isCiBuild;
    private bool m_forceCleanCiCache;
    private WebglCiOptions m_ciOptions;
    private const string CiMetadataAsset = "Assets/Resources/ThreeKingzWebglBuild.json";
    private const string CiHtmlTemplate = "Assets/_Core/Scripts/Editor/WebglCiTemplate/index.html";

    private sealed class WebglCiOptions
    {
        public string output;
        public string publicUrl;
        public string gitSha;
        public int buildNumber;
        public string appVersion;
        public string codeGeneration;
        public bool stripEngineCode;
    }

    private static string RequiredCiArgument(string name)
    {
        var args = Environment.GetCommandLineArgs();
        var positions = Enumerable.Range(0, args.Length).Where(i => args[i] == name).ToArray();
        if (positions.Length != 1 || positions[0] + 1 >= args.Length || string.IsNullOrWhiteSpace(args[positions[0] + 1]))
            throw new ArgumentException("Missing or duplicate argument: " + name);
        return args[positions[0] + 1];
    }

    private static WebglCiOptions ReadWebglCiOptions()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("CI entrypoints require batch mode.");
        if (Application.unityVersion != "6000.3.5f2") throw new InvalidOperationException("Unity 6000.3.5f2 is required.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            throw new InvalidOperationException("Start Unity with -buildTarget WebGL.");
        var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var output = Path.GetFullPath(RequiredCiArgument("-ciOutput"));
        var expectedParent = Path.GetFullPath(Path.Combine(project, ".ci-webgl")) + Path.DirectorySeparatorChar;
        if (!output.StartsWith(expectedParent, StringComparison.Ordinal))
            throw new InvalidOperationException("CI output must be inside this project's .ci-webgl directory.");
        var publicUrl = RequiredCiArgument("-ciPublicUrl").TrimEnd('/');
        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("A public file-server origin is required.");
        var sha = RequiredCiArgument("-ciGitSha");
        if (!Regex.IsMatch(sha, "^[a-f0-9]{40}$")) throw new InvalidOperationException("An exact client commit is required.");
        if (!int.TryParse(RequiredCiArgument("-buildNumber"), NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number <= 0)
            throw new InvalidOperationException("A positive build number is required.");
        var version = PlayerSettings.bundleVersion;
        if (!Regex.IsMatch(version, "^[0-9]+\\.[0-9]+\\.[0-9]+$")) throw new InvalidOperationException("Keep a complete major.minor.patch app version in master.");
        if (!File.Exists(CiHtmlTemplate)) throw new FileNotFoundException("CI HTML template is missing.", CiHtmlTemplate);
        var arguments = Environment.GetCommandLineArgs();
        var codeGeneration = arguments.Contains("-ciCodeGeneration") ? RequiredCiArgument("-ciCodeGeneration") : "project";
        if (codeGeneration != "project" && codeGeneration != "size")
            throw new ArgumentException("CI code generation must be project or size.");
        return new WebglCiOptions
        {
            output = output, publicUrl = publicUrl, gitSha = sha, buildNumber = number, appVersion = version,
            codeGeneration = codeGeneration, stripEngineCode = arguments.Contains("-ciStripEngineCode")
        };
    }

    private static EditorWindow_Build CreateCiBuilder(WebglCiOptions options)
    {
        var builder = CreateInstance<EditorWindow_Build>();
        builder.m_isCiBuild = true;
        builder.m_ciOptions = options;
        builder.m_forceCleanCiCache = Environment.GetCommandLineArgs().Contains("-ciCleanCache");
        var buildData = Resources.Load<TextAsset>("EditorData/BuildData");
        if (buildData == null) throw new InvalidOperationException("EditorData/BuildData is missing.");
        var values = JsonConvert.DeserializeObject<Dictionary<string, long>>(buildData.text);
        if (values == null || !values.TryGetValue("seanson_index", out var season))
            throw new InvalidOperationException("BuildData.seanson_index is missing.");
        builder.SetUserData(new UserData
        {
            buildTarget = BuildTarget.WebGL,
            serviceType = ServiceType.Dev,
            version = options.appVersion,
            seasonIndex = checked((int)season),
            isStore = false,
            isResetPP = false
        });
        return builder;
    }

    // Older runners can still prepare and build in separate processes.
    public static void ConfigureWebglCi()
    {
        try { PrepareWebglCi(ReadWebglCiOptions()); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void PrepareWebglCi(WebglCiOptions options)
    {
        var builder = CreateCiBuilder(options);
        builder.Run(false, false);
        if (options.codeGeneration == "size")
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
        if (options.stripEngineCode) PlayerSettings.stripEngineCode = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        var metadata = new WebglBuildMetadata
        {
            app_version = options.appVersion, git_sha = options.gitSha,
            bundle_base_url = options.publicUrl + "/Bundle/WebGL/ci", bundle_idx = options.buildNumber
        };
        var contents = JsonUtility.ToJson(metadata, true);
        if (!File.Exists(CiMetadataAsset) || File.ReadAllText(CiMetadataAsset) != contents)
        {
            File.WriteAllText(CiMetadataAsset, contents, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(CiMetadataAsset, ImportAssetOptions.ForceSynchronousImport);
        }
        Debug.Log("[WEBGL_METADATA_GUID] " + AssetDatabase.AssetPathToGUID(CiMetadataAsset));
        Debug.Log("[WEBGL_OPTIONS] " + JsonConvert.SerializeObject(DescribeCiOptimization()));
        AssetDatabase.SaveAssets();
        DestroyImmediate(builder);
        Debug.Log("[WEBGL_STAGE] configured");
    }

    public static void BuildWebglCi()
    {
        try { BuildPreparedWebglCi(ReadWebglCiOptions()); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    public static void ExecuteWebglCi()
    {
        try
        {
            var options = ReadWebglCiOptions();
            var before = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL);
            PrepareWebglCi(options);
            var after = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL);
            if (before != after || EditorApplication.isCompiling)
            {
                Debug.Log("[WEBGL_RELOAD_REQUIRED] Compile the prepared defines in a fresh Editor process.");
                EditorApplication.Exit(10);
                return;
            }
            BuildPreparedWebglCi(options);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    private static void BuildPreparedWebglCi(WebglCiOptions options)
    {
        if (Directory.Exists(options.output)) throw new InvalidOperationException("CI output already exists; never overwrite a build.");
        var metadataAsset = Resources.Load<TextAsset>("ThreeKingzWebglBuild");
        if (metadataAsset == null) throw new InvalidOperationException("Run ConfigureWebglCi first.");
        var metadata = JsonUtility.FromJson<WebglBuildMetadata>(metadataAsset.text);
        if (metadata.bundle_idx != options.buildNumber || metadata.app_version != options.appVersion || metadata.git_sha != options.gitSha)
            throw new InvalidOperationException("CI build metadata does not match this request.");
        if (options.codeGeneration == "size" && PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL) != Il2CppCodeGeneration.OptimizeSize)
            throw new InvalidOperationException("CI IL2CPP setting was not prepared.");
        if (options.stripEngineCode && !PlayerSettings.stripEngineCode)
            throw new InvalidOperationException("CI engine stripping setting was not prepared.");
        var builder = CreateCiBuilder(options);
        builder.Run(true, true);
        if (!m_isSuccessBuild) throw new InvalidOperationException("A required WebGL or Addressables build failed.");
        if (PlayerSettings.bundleVersion != options.appVersion) throw new InvalidOperationException("The app version changed during the build.");
        ExportWebglCi(options);
        DestroyImmediate(builder);
        Debug.Log("[WEBGL_STAGE] complete");
    }

    private static object DescribeCiOptimization()
    {
        return new
        {
            il2cpp_code_generation = PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL).ToString(),
            strip_engine_code = PlayerSettings.stripEngineCode
        };
    }

    private void RecordCiBuildReport(BuildReport report, string variant)
    {
        if (!m_isCiBuild) return;
        var directory = Path.Combine(m_ciOptions.output, "reports");
        Directory.CreateDirectory(directory);
        var data = new
        {
            build_number = m_ciOptions.buildNumber, client_sha = m_ciOptions.gitSha, variant,
            result = report.summary.result.ToString(), duration_seconds = report.summary.totalTime.TotalSeconds,
            size_bytes = report.summary.totalSize, errors = report.summary.totalErrors,
            optimization = DescribeCiOptimization(), metadata_guid = AssetDatabase.AssetPathToGUID(CiMetadataAsset),
            steps = report.steps.Select(step => new { step.name, step.depth, duration_seconds = step.duration.TotalSeconds }).ToArray()
        };
        File.WriteAllText(Path.Combine(directory, variant + ".json"), JsonConvert.SerializeObject(data, Formatting.Indented), new UTF8Encoding(false));
        var trace = Path.Combine("Library", "Bee", "buildreport.json");
        if (File.Exists(trace)) File.Copy(trace, Path.Combine(directory, variant + "-trace.json"), false);
        Debug.Log($"[WEBGL_REPORT] {variant} seconds={report.summary.totalTime.TotalSeconds:F3} result={report.summary.result}");
    }

    private static void CopyCiDirectory(string source, string destination)
    {
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(file, target, false);
        }
    }

    private static object[] DescribeCiFiles(string directory)
    {
        return Directory.GetFiles(directory, "*", SearchOption.AllDirectories).OrderBy(file => file, StringComparer.Ordinal).Select(file =>
        {
            var info = new FileInfo(file);
            if (info.Length == 0) throw new InvalidOperationException("Empty build artifact: " + file);
            string hash;
            using (var stream = File.OpenRead(file))
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            return (object)new { path = file.Substring(directory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/'), size = info.Length, sha256 = hash };
        }).ToArray();
    }

    private static void ExportWebglCi(WebglCiOptions options)
    {
        Debug.Log("[WEBGL_STAGE] export");
        var player = Path.Combine(options.output, "webgl");
        var bundles = Path.Combine(options.output, "bundles");
        CopyCiDirectory(Path.GetFullPath("0_Bin/WebGL/Dev/web"), Path.Combine(player, "web"));
        CopyCiDirectory(Path.GetFullPath("Bundle/_Last/WebGL"), bundles);
        var html = File.ReadAllText(CiHtmlTemplate);
        var buildIndex = JsonConvert.SerializeObject("ci/" + options.buildNumber);
        html = Regex.Replace(html, @"let version_BuildIndex = [^;]+;", "let version_BuildIndex = " + buildIndex + ";");
        html = Regex.Replace(html, @"companyName: ""[^""]*""", "companyName: " + JsonConvert.SerializeObject(PlayerSettings.companyName));
        html = Regex.Replace(html, @"productName: ""[^""]*""", "productName: " + JsonConvert.SerializeObject(PlayerSettings.productName));
        html = Regex.Replace(html, @"productVersion: ""[^""]*""", "productVersion: " + JsonConvert.SerializeObject(options.appVersion));
        html = Regex.Replace(html, @"<title>.*?</title>", "<title>ThreeKingz</title>", RegexOptions.Singleline);
        if (!html.Contains("let version_BuildIndex = " + buildIndex)) throw new InvalidOperationException("CI HTML build marker was not replaced.");
        File.WriteAllText(Path.Combine(player, "index.html"), html, new UTF8Encoding(false));
        var files = DescribeCiFiles(player);
        var bundleFiles = DescribeCiFiles(bundles);
        if (files.Length < 5 || bundleFiles.Length == 0) throw new InvalidOperationException("Incomplete WebGL artifacts.");
        var manifest = new
        {
            format_version = 1, build_number = options.buildNumber, client_sha = options.gitSha,
            app_version = options.appVersion, unity_version = Application.unityVersion, branch = "master",
            bundle_idx = options.buildNumber, created_at = DateTime.UtcNow.ToString("O"),
            variants = new[] { "DXT", "ASTC" }, optimization = DescribeCiOptimization(), files, bundles = bundleFiles
        };
        File.WriteAllText(Path.Combine(player, "build-manifest.json"), JsonConvert.SerializeObject(manifest, Formatting.Indented), new UTF8Encoding(false));
    }
}
