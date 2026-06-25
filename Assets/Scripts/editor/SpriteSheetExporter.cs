#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SpriteSheetExporter
{
    [MenuItem("Tools/Sprites/Export Selected to Numbered PNGs", true)]
    static bool ValidateExportSelected()
    {
        return GetSelectedTexturePath() != null;
    }

    [MenuItem("Tools/Sprites/Export Selected to Numbered PNGs")]
    public static void ExportSelectedToNumberedPngs()
    {
        var texturePath = GetSelectedTexturePath();
        if (string.IsNullOrEmpty(texturePath))
        {
            EditorUtility.DisplayDialog("导出失败", "请先在 Project 中选中一张已切片的纹理或子 Sprite。", "确定");
            return;
        }

        var sprites = GetSpritesSortedByRect(texturePath);
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "导出失败",
                "该纹理没有子 Sprite。\n请先在 Sprite Editor 中 Slice 并点击 Apply。",
                "确定");
            return;
        }

        var defaultFolder = Path.GetDirectoryName(texturePath)?.Replace('\\', '/') ?? "Assets";
        var outputRoot = EditorUtility.OpenFolderPanel("选择导出文件夹", defaultFolder, "");
        if (string.IsNullOrEmpty(outputRoot))
            return;

        EnsureTextureReadable(texturePath);

        var textureName = Path.GetFileNameWithoutExtension(texturePath);
        var outputDir = Path.Combine(outputRoot, textureName);
        Directory.CreateDirectory(outputDir);

        var sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (sourceTexture == null)
        {
            EditorUtility.DisplayDialog("导出失败", "无法加载纹理：" + texturePath, "确定");
            return;
        }

        for (var i = 0; i < sprites.Count; i++)
        {
            var pngPath = Path.Combine(outputDir, (i + 1) + ".png");
            var extracted = ExtractSpriteTexture(sourceTexture, sprites[i]);
            File.WriteAllBytes(pngPath, extracted.EncodeToPNG());
            Object.DestroyImmediate(extracted);
        }

        if (IsPathUnderAssets(outputRoot))
            AssetDatabase.Refresh();

        Debug.Log($"已导出 {sprites.Count} 张 PNG 到：{outputDir.Replace('\\', '/')}");
    }

    static string GetSelectedTexturePath()
    {
        if (Selection.objects == null || Selection.objects.Length != 1)
            return null;

        var selected = Selection.activeObject;
        if (selected is Sprite sprite && sprite.texture != null)
            return AssetDatabase.GetAssetPath(sprite.texture);

        var path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path))
            return null;

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
            return path;

        return null;
    }

    static List<Sprite> GetSpritesSortedByRect(string texturePath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .Where(s => s.rect.width > 0 && s.rect.height > 0)
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToList();
    }

    static void EnsureTextureReadable(string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null || importer.isReadable)
            return;

        importer.isReadable = true;
        importer.SaveAndReimport();
    }

    static Texture2D ExtractSpriteTexture(Texture2D source, Sprite sprite)
    {
        var rect = sprite.textureRect;
        var x = Mathf.RoundToInt(rect.x);
        var y = Mathf.RoundToInt(rect.y);
        var width = Mathf.RoundToInt(rect.width);
        var height = Mathf.RoundToInt(rect.height);

        var extracted = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = source.GetPixels(x, y, width, height);
        extracted.SetPixels(pixels);
        extracted.Apply();
        return extracted;
    }

    static bool IsPathUnderAssets(string absolutePath)
    {
        var dataPath = Application.dataPath.Replace('\\', '/');
        var normalized = absolutePath.Replace('\\', '/');
        return normalized.StartsWith(dataPath);
    }
}
#endif
