using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;

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
        var outputLocation = outputDirectoryRoot;
        var buildTarget = "windows";
        foreach (var arg in arguments)
        {
            if (arg.StartsWith("--assetBundleName="))
            {
                assetBundleName = arg.Substring("--assetBundleName=".Length);
                Debug.Log($"Using asset bundle name: {assetBundleName}");
            }

            if (arg.StartsWith("--outputLocation"))
            {
                outputLocation = arg.Substring("--outputLocation=".Length).Replace('\\', Path.DirectorySeparatorChar);
                if (!outputLocation.StartsWith("/"))
                {
                    outputLocation = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), outputLocation));
                }
                Debug.Log($"Using output location: {outputLocation}");
            }

            if (arg.StartsWith("--buildTarget"))
            {
                buildTarget = arg.Substring("--buildTarget=".Length);
                Debug.Log($"Using build target: {buildTarget}");
            }
        }

        // Ensure textures are labeled correctly before proceeding.
        var count = AssetLabeler.LabelAllAssetsWithCommonName(assetBundleName);
        if (count == 0)
        {
            Debug.Log("[Error] No assets were labeled; aborting asset bundle build.");
            return;
        }

        // Since the bundle only includes generic assets like textures or sounds
        // and not platform-specific assets, we can build for all platforms.
        Debug.Log("Building asset bundle.");

        // Build the asset bundles with LZ4 compression.
        foreach (var target in supportedTargets.Where(t => t.Key == buildTarget).ToList())
        {
            if (!Directory.Exists(outputDirectoryRoot)) Directory.CreateDirectory(outputDirectoryRoot);

            // Build the bundle
            BuildPipeline.BuildAssetBundles(
                outputDirectoryRoot,
                BuildAssetBundleOptions.ChunkBasedCompression,
                target.Value
            );

            File.Delete(Path.Combine(outputDirectoryRoot, target.Key));
            File.Delete(Path.Combine(outputDirectoryRoot, target.Key + ".manifest"));

            // Rename bundle and manifest
            string sourceBundlePath = Path.Combine(outputDirectoryRoot, assetBundleName);
            string sourceManifestPath = sourceBundlePath + ".manifest";

            string bundleName = assetBundleName.ToLowerInvariant().Replace('.', '_').Replace("cryptiklemur", "resource") + $"_{target.Key}";
            string renamedBundlePath = Path.Combine(outputDirectoryRoot, bundleName);
            string renamedManifestPath = renamedBundlePath + ".manifest";

            if (Directory.Exists(outputLocation)) Directory.Delete(outputLocation, true);
            if (!Directory.Exists(outputLocation)) Directory.CreateDirectory(outputLocation);
            if (File.Exists(sourceBundlePath))
            {
                if (File.Exists(renamedBundlePath)) File.Delete(renamedBundlePath);
                File.Move(sourceBundlePath, renamedBundlePath);
                File.Copy(renamedBundlePath, Path.Combine(outputLocation, bundleName), true);
            }
            if (File.Exists(sourceManifestPath))
            {
                if (File.Exists(renamedManifestPath)) File.Delete(renamedManifestPath);
                File.Move(sourceManifestPath, renamedManifestPath);
                File.Copy(renamedManifestPath, Path.Combine(outputLocation, bundleName + ".manifest"), true);
            }

        }

        Debug.Log("Asset bundles built successfully.");
    }
}