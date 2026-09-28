using System;
using UnityEngine;

[Serializable]
public sealed class WebglBuildMetadata
{
    public string app_version;
    public string git_sha;
    public string bundle_base_url;
    public int bundle_idx;

    public static string ResolveBundleUrl()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var asset = Resources.Load<TextAsset>("ThreeKingzWebglBuild");
        if (asset != null)
        {
            var metadata = JsonUtility.FromJson<WebglBuildMetadata>(asset.text);
            if (metadata == null || metadata.app_version != Application.version ||
                metadata.bundle_idx <= 0 ||
                !Uri.TryCreate(metadata.bundle_base_url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment))
                throw new InvalidOperationException("Invalid WebGL build metadata.");
            return metadata.bundle_base_url.TrimEnd('/') + "/" + metadata.bundle_idx;
        }
#endif
        // Existing manual builds retain their established version-based bundle path.
        return "https://dev-static.kingz.app/Bundle/WebGL/" + Application.version.Split('.')[2];
    }
}
