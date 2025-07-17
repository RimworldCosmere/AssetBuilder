using System;
using UnityEditor;
using UnityEngine;

public class ModAssetBundleBuilder
{
    private const string outputDirectoryRoot = "Assets/AssetBundles";
    private static BuildTarget[] supportedTargets = new BuildTarget[]
    {
        BuildTarget.StandaloneWindows64,
        //BuildTarget.StandaloneOSX,
        //BuildTarget.StandaloneLinux64
    };

    [MenuItem("Assets/Build Compressed Asset Bundle (LZ4)")]
    public static void BuildBundles()
    {
        var arguments = Environment.GetCommandLineArgs();
        var assetBundleName = "assetBundle";
        foreach (var arg in arguments)
        {
            if (!arg.StartsWith("--assetBundleName="))
            {
                continue;
            }

            assetBundleName = arg.Substring("--assetBundleName=".Length);
            Debug.Log($"Using asset bundle name: {assetBundleName}");
        }

        // Ensure textures are labeled correctly before proceeding.
        var count = AssetLabeler.LabelAllAssetsWithCommonName(assetBundleName);
        if (count == 0)
        {
            Debug.LogError("No assets were labeled; aborting asset bundle build.");
            return;
        }

        // Since the bundle only includes generic assets like textures or sounds
        // and not platform-specific assets, we can build for all platforms.
        Debug.Log("Building asset bundle.");

        // Build the asset bundles with LZ4 compression.
        foreach (var target in supportedTargets) {
            BuildPipeline.BuildAssetBundles(
                outputDirectoryRoot,
                BuildAssetBundleOptions.ChunkBasedCompression,
                target
            );
        }

        Debug.Log("Asset bundles built successfully.");
    }
}