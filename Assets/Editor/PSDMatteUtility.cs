using System.IO;
using UnityEditor;
using UnityEditor.U2D.PSD;
using UnityEngine;

public static class PSDMatteUtility
{
    private const string PropRemoveMatte = "m_PSDRemoveMatte";
    private const string PropShowRemoveMatteOption = "m_PSDShowRemoveMatteOption";

    /// <summary>
    /// Ensures PSD matte settings for a single asset. Path must be project-relative (start with "Assets/").
    /// Returns true if properties were changed and the asset was reimported.
    /// </summary>
    public static bool EnsureSettingsForFile(string projectRelativeAssetPath)
    {
        if (string.IsNullOrWhiteSpace(projectRelativeAssetPath))
            throw new System.ArgumentException(nameof(projectRelativeAssetPath));

        var path = projectRelativeAssetPath.Replace("\\", "/");
        if (!path.StartsWith("Assets/") && path != "Assets")
            throw new System.ArgumentException("Path must be under the project Assets folder: " + path);

        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        if (ext is not ".psd" and not ".psb")
            return false; // not a PSD/PSB – nothing to do

        var importer = AssetImporter.GetAtPath(path) as PSDImporter;
        if (importer == null)
            return false; // not handled by PSDImporter (unexpected) – bail safely

        var so = new SerializedObject(importer);
        bool changed = false;

        var remove = so.FindProperty(PropRemoveMatte);
        if (remove != null && remove.boolValue != true) { remove.boolValue = true; changed = true; }

        var show = so.FindProperty(PropShowRemoveMatteOption);
        if (show != null && show.boolValue != false) { show.boolValue = false; changed = true; }

        if (!changed) return false;

        so.ApplyModifiedPropertiesWithoutUndo();
        importer.SaveAndReimport(); // triggers a fresh import with the new flags
        return true;
    }

    /// <summary>
    /// Convenience overload for GUIDs from AssetDatabase/Selections.
    /// </summary>
    public static bool EnsureSettingsForGuid(string guid)
        => EnsureSettingsForFile(AssetDatabase.GUIDToAssetPath(guid));
}
