using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Xml.Linq;

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
        Debug.Log($"Starting AssetBundle Builder");
        var arguments = Environment.GetCommandLineArgs();
        var sourceDirectory = "";
        var buildTarget = "all";
        foreach (var arg in arguments)
        {
            if (arg.StartsWith("-buildTarget="))
            {
                buildTarget = arg.Substring("-buildTarget=".Length);
                Debug.Log($"Using build target: {buildTarget}");
            }

            if (arg.StartsWith("-source="))
            {
                sourceDirectory = arg.Substring("-source=".Length);
                if (!sourceDirectory.StartsWith("/"))
                {
                    sourceDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), sourceDirectory));
                }
                Debug.Log($"Using source directory: {sourceDirectory}");
            }
        }

        if (string.IsNullOrEmpty(sourceDirectory))
        {
            throw new Exception("Source directory must be set with -source=.");
        }

        if (!Directory.Exists(Path.Combine(sourceDirectory, "About")))
        {
            throw new Exception($"{sourceDirectory} doesn't look like a valid mod. Missing About directory");
        }

        var assetBundleName = LoadPackageId(sourceDirectory);
        if (string.IsNullOrEmpty(assetBundleName))
        {
            throw new Exception("Failed to find packageId. Is one set in your About/About.xml?");
        }

        var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", assetBundleName);
        if (!Directory.Exists(expectedPath)) {
            throw new Exception($"Expected assets in {expectedPath}. See the readme.");
        }

        // Ensure textures are labeled correctly before proceeding.
        var bundle = AssetLabeler.LabelAllAssetsWithCommonName(assetBundleName);
        if (bundle.assetNames == null || bundle.assetNames.Count() == 0) {
            throw new Exception("No assets were labeled; aborting asset bundle build.");
        }

        var outputLocation = Path.Combine(sourceDirectory, "AssetBundles");

        // Since the bundle only includes generic assets like textures or sounds
        // and not platform-specific assets, we can build for all platforms.
        Debug.Log($"Building asset bundle in {outputLocation}.");

        // Build the asset bundles with LZ4 compression.
        var targets = buildTarget == "all" ? supportedTargets : supportedTargets.Where(t => t.Key == buildTarget);
        foreach (var target in targets)
        {
            if (Directory.Exists(outputLocation)) Directory.Delete(outputLocation, true);
            if (!Directory.Exists(outputLocation)) Directory.CreateDirectory(outputLocation);

            // Build the bundle
            var bundles = new AssetBundleBuild[1];
            bundles[0] = bundle;

            var manifest = BuildPipeline.BuildAssetBundles(new BuildAssetBundlesParameters
            {
                outputPath = outputLocation,
                options = BuildAssetBundleOptions.ChunkBasedCompression,
                bundleDefinitions = bundles,
                targetPlatform = target.Value
            });

            File.Delete(Path.Combine(outputLocation, "AssetBundles"));
            File.Delete(Path.Combine(outputLocation, "AssetBundles.manifest"));
            File.Move(Path.Combine(outputLocation, assetBundleName), Path.Combine(outputLocation, $"resource_{assetBundleName.Replace(".", "_")}_{target.Key}"));
            File.Move(Path.Combine(outputLocation, assetBundleName + ".manifest"), Path.Combine(outputLocation, $"resource_{assetBundleName.Replace(".", "_")}_{target.Key}.manifest"));
        }

        Debug.Log("Asset bundles built successfully.");
    }

    private static string LoadPackageId(string sourceDirectory)
    {
        FileStream fs = new FileStream(Path.Combine(sourceDirectory, "About", "About.xml"), FileMode.Open, FileAccess.Read);
        var doc = XDocument.Load(fs);
        if (doc.Root == null) return "";
        var packageId = doc.Root.Element("packageId");
        if (packageId == null || packageId.Value == null) return "";

        return packageId.Value.ToLower();
    }
}