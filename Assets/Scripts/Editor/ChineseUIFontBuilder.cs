#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class ChineseUIFontBuilder
{
    const string FontSourcePath = "Assets/Fonts/NotoSansSC-Regular.ttf";
    const string FontAssetPath = "Assets/Fonts/NotoSansSC SDF.asset";
    const string LiberationFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    public static TMP_FontAsset EnsureChineseUIFont()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
            return existing;

        return BuildFontAsset();
    }

    [MenuItem("Game/Setup/Build Chinese UI Font")]
    public static TMP_FontAsset BuildFontAsset()
    {
        return EnsureOrBuildFont(forceRebuild: false);
    }

    [MenuItem("Game/Setup/Rebuild Chinese UI Font")]
    public static TMP_FontAsset RebuildFontAsset()
    {
        return EnsureOrBuildFont(forceRebuild: true);
    }

    static TMP_FontAsset EnsureOrBuildFont(bool forceRebuild)
    {
        EnsureFolder("Assets/Fonts");

        if (!File.Exists(FontSourcePath))
        {
            Debug.LogError($"缺少字体文件: {FontSourcePath}");
            return null;
        }

        if (!forceRebuild)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
                return existing;
        }

        AssetDatabase.ImportAsset(FontSourcePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceUpdate);

        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            Debug.LogError($"缺少 TMP 字体资产: {FontAssetPath}");
            return null;
        }

        ApplyTmpSettings(fontAsset);
        AssetDatabase.SaveAssets();
        if (forceRebuild)
            AssetDatabase.Refresh();

        Debug.Log(forceRebuild
            ? $"已重建中文字体资产: {FontAssetPath}"
            : $"已配置中文字体资产: {FontAssetPath}");
        return fontAsset;
    }

    static void ApplyTmpSettings(TMP_FontAsset fontAsset)
    {
        var settingsObjects = AssetDatabase.LoadAllAssetsAtPath(TmpSettingsPath);
        if (settingsObjects == null || settingsObjects.Length == 0)
            return;

        var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationFontPath);
        var so = new SerializedObject(settingsObjects[0]);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;

        var fallbackProp = so.FindProperty("m_fallbackFontAssets");
        if (liberation != null)
        {
            fallbackProp.arraySize = 1;
            fallbackProp.GetArrayElementAtIndex(0).objectReferenceValue = liberation;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settingsObjects[0]);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
