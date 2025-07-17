using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ModAssetBundleBuilder
{
    private const string outputDirectoryRoot = "Assets/AssetBundles";
    private static Dictionary<string, BuildTarget> supportedTargets = new Dictionary<string, BuildTarget>()
    {
        { "windows", BuildTarget.StandaloneWindows64},
        { "mac", BuildTarget.StandaloneOSX},
        { "linux", BuildTarget.StandaloneLinux64},
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
        foreach (var target in supportedTargets)
        {
            string targetOutputDir = $"{outputDirectoryRoot}/{target.Key}";
            if (!System.IO.Directory.Exists(targetOutputDir))
            {
                System.IO.Directory.CreateDirectory(targetOutputDir);
            }

            // Build the bundle
            BuildPipeline.BuildAssetBundles(
                targetOutputDir,
                BuildAssetBundleOptions.ChunkBasedCompression,
                target.Value
            );

            // Rename bundle and manifest
            string sourceBundlePath = System.IO.Path.Combine(targetOutputDir, assetBundleName);
            string sourceManifestPath = sourceBundlePath + ".manifest";
            string renamedBundlePath = System.IO.Path.Combine(targetOutputDir, $"{assetBundleName}_{target.Key}");
            string renamedManifestPath = renamedBundlePath + ".manifest";

            if (File.Exists(sourceBundlePath))
            {
                if (File.Exists(renamedBundlePath)) {
                    File.Delete(renamedBundlePath);
                }
                File.Move(sourceBundlePath, renamedBundlePath);
            }
            if (File.Exists(sourceManifestPath))
            {
                if (File.Exists(renamedManifestPath)) {
                    File.Delete(renamedManifestPath);
                }
                File.Move(sourceManifestPath, renamedManifestPath);
            }
        }

        Debug.Log("Asset bundles built successfully.");
    }
}