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
using UnityEngine;

public partial class EditorWindow_Build
{
    private bool m_isCiBuild;
    private bool m_forceCleanCiCache;
    private const string CiMetadataAsset = "Assets/Resources/ThreeKingzWebglBuild.json";
    private const string CiHtmlTemplate = "Assets/_Core/Scripts/Editor/WebglCiTemplate/index.html";

    private sealed class WebglCiOptions
    {
        public string output;
        public string publicUrl;
        public string gitSha;
        public int buildNumber;
        public string appVersion;
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
        return new WebglCiOptions { output = output, publicUrl = publicUrl, gitSha = sha, buildNumber = number, appVersion = version };
    }

    private static EditorWindow_Build CreateCiBuilder(WebglCiOptions options)
    {
        var builder = CreateInstance<EditorWindow_Build>();
        builder.m_isCiBuild = true;
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

    // Preparation is a separate process so define changes compile before the build entrypoint.
    public static void ConfigureWebglCi()
    {
        try
        {
            var options = ReadWebglCiOptions();
            var builder = CreateCiBuilder(options);
            builder.Run(false, false);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            var metadata = new WebglBuildMetadata
            {
                app_version = options.appVersion, git_sha = options.gitSha,
                bundle_base_url = options.publicUrl + "/Bundle/WebGL/ci", bundle_idx = options.buildNumber
            };
            File.WriteAllText(CiMetadataAsset, JsonUtility.ToJson(metadata, true), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(CiMetadataAsset, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            DestroyImmediate(builder);
            Debug.Log("[WEBGL_STAGE] configured");
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    public static void BuildWebglCi()
    {
        try
        {
            var options = ReadWebglCiOptions();
            if (Directory.Exists(options.output)) throw new InvalidOperationException("CI output already exists; never overwrite a build.");
            var metadataAsset = Resources.Load<TextAsset>("ThreeKingzWebglBuild");
            if (metadataAsset == null) throw new InvalidOperationException("Run ConfigureWebglCi first.");
            var metadata = JsonUtility.FromJson<WebglBuildMetadata>(metadataAsset.text);
            if (metadata.bundle_idx != options.buildNumber || metadata.app_version != options.appVersion || metadata.git_sha != options.gitSha)
                throw new InvalidOperationException("CI build metadata does not match this request.");
            var builder = CreateCiBuilder(options);
            builder.Run(true, true);
            if (!m_isSuccessBuild) throw new InvalidOperationException("A required WebGL or Addressables build failed.");
            if (PlayerSettings.bundleVersion != options.appVersion) throw new InvalidOperationException("The app version changed during the build.");
            ExportWebglCi(options);
            DestroyImmediate(builder);
            Debug.Log("[WEBGL_STAGE] complete");
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
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
            variants = new[] { "DXT", "ASTC" }, files, bundles = bundleFiles
        };
        File.WriteAllText(Path.Combine(player, "build-manifest.json"), JsonConvert.SerializeObject(manifest, Formatting.Indented), new UTF8Encoding(false));
    }
}
