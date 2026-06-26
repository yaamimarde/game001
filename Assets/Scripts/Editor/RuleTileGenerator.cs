#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class RuleTileGeneratorWindow : EditorWindow
{
    const string DefaultFolder =
        "Assets/material/Era of Fantasy Grasslands - Pixelart Asset Pack/Tileset/Tilesheet_Shadow";

    const string DefaultTemplatePath = DefaultFolder + "/Grass Rule Tile.asset";
    const string DefaultReferencePath = DefaultFolder + "/1.png";

    RuleTile _templateRuleTile;
    Texture2D _referenceAutotile;
    Texture2D _targetAutotile;
    Texture2D _fillTexture;
    DefaultAsset _outputFolder;
    string _outputName = string.Empty;

    Vector2 _batchScroll;
    readonly List<BatchRow> _batchRows = new List<BatchRow>();

    class BatchRow
    {
        public int Number;
        public bool Enabled;
        public Texture2D FillTexture;
    }

    [MenuItem("Tools/Tiles/Generate Rule Tile From Template")]
    public static void ShowWindow()
    {
        var window = GetWindow<RuleTileGeneratorWindow>("Rule Tile Generator");
        window.minSize = new Vector2(420f, 520f);
    }

    void OnEnable()
    {
        if (_templateRuleTile == null)
            _templateRuleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(DefaultTemplatePath);

        if (_referenceAutotile == null)
            _referenceAutotile = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultReferencePath);

        if (_outputFolder == null)
            _outputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultFolder);

        EnsureBatchRows();
    }

    void EnsureBatchRows()
    {
        if (_batchRows.Count > 0)
            return;

        for (var n = 2; n <= 16; n++)
        {
            _batchRows.Add(new BatchRow
            {
                Number = n,
                Enabled = false,
                FillTexture = null,
            });
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Rule Tile Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        _templateRuleTile = (RuleTile)EditorGUILayout.ObjectField(
            "Template Rule Tile", _templateRuleTile, typeof(RuleTile), false);
        _referenceAutotile = (Texture2D)EditorGUILayout.ObjectField(
            "Reference Autotile", _referenceAutotile, typeof(Texture2D), false);
        _targetAutotile = (Texture2D)EditorGUILayout.ObjectField(
            "Target Autotile", _targetAutotile, typeof(Texture2D), false);
        _fillTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Fill Sprite Texture", _fillTexture, typeof(Texture2D), false);
        _outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Output Folder", _outputFolder, typeof(DefaultAsset), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel("Output Asset Name");
            _outputName = EditorGUILayout.TextField(_outputName);
            if (GUILayout.Button("Auto", GUILayout.Width(48f)))
                _outputName = RuleTileGenerator.GetDefaultOutputName(_targetAutotile);
        }

        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(!CanGenerateSingle()))
        {
            if (GUILayout.Button("Generate One", GUILayout.Height(28f)))
                GenerateSingle();
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Batch Generate 2–16", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "勾选要生成的行，并为每行指定填充图。Autotile 将自动使用同目录下的 N.png。",
            MessageType.Info);

        _batchScroll = EditorGUILayout.BeginScrollView(_batchScroll, GUILayout.Height(220f));
        foreach (var row in _batchRows)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                row.Enabled = EditorGUILayout.Toggle(row.Enabled, GUILayout.Width(18f));
                EditorGUILayout.LabelField($"{row.Number}.png", GUILayout.Width(52f));
                row.FillTexture = (Texture2D)EditorGUILayout.ObjectField(
                    row.FillTexture, typeof(Texture2D), false);
            }
        }
        EditorGUILayout.EndScrollView();

        using (new EditorGUI.DisabledScope(!CanGenerateBatch()))
        {
            if (GUILayout.Button("Batch Generate Selected", GUILayout.Height(28f)))
                GenerateBatch();
        }
    }

    bool CanGenerateSingle()
    {
        return _templateRuleTile != null
            && _referenceAutotile != null
            && _targetAutotile != null
            && _fillTexture != null
            && GetOutputFolderPath() != null;
    }

    bool CanGenerateBatch()
    {
        if (_templateRuleTile == null || _referenceAutotile == null || GetOutputFolderPath() == null)
            return false;

        return _batchRows.Any(r => r.Enabled && r.FillTexture != null);
    }

    string GetOutputFolderPath()
    {
        if (_outputFolder == null)
            return null;

        var path = AssetDatabase.GetAssetPath(_outputFolder);
        return string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path) ? null : path;
    }

    void GenerateSingle()
    {
        var outputFolder = GetOutputFolderPath();
        var outputName = string.IsNullOrWhiteSpace(_outputName)
            ? RuleTileGenerator.GetDefaultOutputName(_targetAutotile)
            : _outputName.Trim();
        var outputPath = Path.Combine(outputFolder, outputName + ".asset").Replace('\\', '/');

        if (!RuleTileGenerator.Generate(
                _templateRuleTile,
                AssetDatabase.GetAssetPath(_referenceAutotile),
                AssetDatabase.GetAssetPath(_targetAutotile),
                AssetDatabase.GetAssetPath(_fillTexture),
                outputPath,
                out var error))
        {
            EditorUtility.DisplayDialog("生成失败", error, "确定");
            return;
        }

        EditorUtility.DisplayDialog("生成成功", "已创建：" + outputPath, "确定");
    }

    void GenerateBatch()
    {
        var outputFolder = GetOutputFolderPath();
        var referencePath = AssetDatabase.GetAssetPath(_referenceAutotile);
        var autotileFolder = Path.GetDirectoryName(referencePath)?.Replace('\\', '/');
        var success = 0;
        var errors = new List<string>();

        foreach (var row in _batchRows.Where(r => r.Enabled))
        {
            if (row.FillTexture == null)
            {
                errors.Add($"{row.Number}.png：未指定填充图");
                continue;
            }

            var targetPath = $"{autotileFolder}/{row.Number}.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath) == null)
            {
                errors.Add($"{row.Number}.png：文件不存在");
                continue;
            }

            var outputPath = $"{outputFolder}/{row.Number} Rule Tile.asset";
            if (!RuleTileGenerator.Generate(
                    _templateRuleTile,
                    referencePath,
                    targetPath,
                    AssetDatabase.GetAssetPath(row.FillTexture),
                    outputPath,
                    out var error))
            {
                errors.Add($"{row.Number}.png：{error}");
                continue;
            }

            success++;
        }

        var message = $"成功生成 {success} 个 Rule Tile。";
        if (errors.Count > 0)
            message += "\n\n失败：\n" + string.Join("\n", errors);

        EditorUtility.DisplayDialog(
            errors.Count > 0 ? "批量生成完成（部分失败）" : "批量生成成功",
            message,
            "确定");
    }
}

public static class RuleTileGenerator
{
    const int ExpectedSpriteCount = 15;

    public static string GetDefaultOutputName(Texture2D targetAutotile)
    {
        if (targetAutotile == null)
            return "New Rule Tile";

        var baseName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(targetAutotile));
        return string.IsNullOrEmpty(baseName) ? "New Rule Tile" : baseName + " Rule Tile";
    }

    public static bool Generate(
        RuleTile template,
        string referenceAutotilePath,
        string targetAutotilePath,
        string fillTexturePath,
        string outputAssetPath,
        out string error)
    {
        error = null;

        if (template == null)
        {
            error = "模板 Rule Tile 为空。";
            return false;
        }

        if (string.IsNullOrEmpty(referenceAutotilePath)
            || AssetDatabase.LoadAssetAtPath<Texture2D>(referenceAutotilePath) == null)
        {
            error = "参考 Autotile 无效：" + referenceAutotilePath;
            return false;
        }

        if (string.IsNullOrEmpty(targetAutotilePath)
            || AssetDatabase.LoadAssetAtPath<Texture2D>(targetAutotilePath) == null)
        {
            error = "目标 Autotile 无效：" + targetAutotilePath;
            return false;
        }

        if (!ApplyReferenceSliceSettings(referenceAutotilePath, targetAutotilePath, out error))
            return false;

        if (!TryGetFillSprite(fillTexturePath, out var fillSprite, out error))
            return false;

        var referenceAutotile = AssetDatabase.LoadAssetAtPath<Texture2D>(referenceAutotilePath);
        if (!TryBuildTargetSpritesBySuffix(targetAutotilePath, out var targetBySuffix, out error))
            return false;

        var newTile = CloneRuleTile(template, referenceAutotile, fillSprite, targetBySuffix, out error);
        if (newTile == null)
            return false;

        var outputName = Path.GetFileNameWithoutExtension(outputAssetPath);
        newTile.name = outputName;

        var outputDir = Path.GetDirectoryName(outputAssetPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(outputDir) || !AssetDatabase.IsValidFolder(outputDir))
        {
            error = "输出目录无效：" + outputAssetPath;
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<RuleTile>(outputAssetPath) != null)
            AssetDatabase.DeleteAsset(outputAssetPath);

        AssetDatabase.CreateAsset(newTile, outputAssetPath);
        EditorUtility.SetDirty(newTile);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[RuleTileGenerator] 已生成 {outputAssetPath}\n" +
            $"- 参考：{referenceAutotilePath}\n" +
            $"- 目标：{targetAutotilePath}\n" +
            $"- 填充：{fillTexturePath}\n" +
            $"- 映射子 Sprite 数量：{targetBySuffix.Count}");

        return true;
    }

    static bool ApplyReferenceSliceSettings(
        string referenceAutotilePath,
        string targetAutotilePath,
        out string error)
    {
        error = null;

        var referenceImporter = AssetImporter.GetAtPath(referenceAutotilePath) as TextureImporter;
        var targetImporter = AssetImporter.GetAtPath(targetAutotilePath) as TextureImporter;
        if (referenceImporter == null || targetImporter == null)
        {
            error = "无法读取 TextureImporter。";
            return false;
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();

        var referenceProvider = factory.GetSpriteEditorDataProviderFromObject(referenceImporter);
        if (referenceProvider == null)
        {
            error = "无法读取参考纹理的 Sprite 数据：" + referenceAutotilePath;
            return false;
        }

        referenceProvider.InitSpriteEditorDataProvider();
        var referenceRects = referenceProvider.GetSpriteRects()
            .OrderByDescending(r => r.rect.y)
            .ThenBy(r => r.rect.x)
            .ToArray();

        if (referenceRects.Length != ExpectedSpriteCount)
        {
            error =
                $"参考纹理应包含 {ExpectedSpriteCount} 个子 Sprite，实际为 {referenceRects.Length}：{referenceAutotilePath}";
            return false;
        }

        var targetBaseName = Path.GetFileNameWithoutExtension(targetAutotilePath);
        var targetRects = new SpriteRect[referenceRects.Length];
        for (var i = 0; i < referenceRects.Length; i++)
        {
            var referenceRect = referenceRects[i];
            var suffix = GetSpriteSuffix(referenceRect.name);
            targetRects[i] = new SpriteRect
            {
                name = $"{targetBaseName}_{suffix}",
                rect = referenceRect.rect,
                alignment = referenceRect.alignment,
                pivot = referenceRect.pivot,
                border = referenceRect.border,
            };
        }

        targetImporter.textureType = TextureImporterType.Sprite;
        targetImporter.spriteImportMode = SpriteImportMode.Multiple;
        targetImporter.filterMode = FilterMode.Point;
        targetImporter.spritePixelsPerUnit = referenceImporter.spritePixelsPerUnit;
        targetImporter.alphaIsTransparency = true;

        var targetProvider = factory.GetSpriteEditorDataProviderFromObject(targetImporter);
        if (targetProvider == null)
        {
            error = "无法写入目标纹理的 Sprite 数据：" + targetAutotilePath;
            return false;
        }

        targetProvider.InitSpriteEditorDataProvider();
        targetProvider.SetSpriteRects(targetRects);
        targetProvider.Apply();

        EditorUtility.SetDirty(targetImporter);
        targetImporter.SaveAndReimport();

        Debug.Log($"[RuleTileGenerator] 已切片 {targetAutotilePath}（{ExpectedSpriteCount} 个子 Sprite，PPU={targetImporter.spritePixelsPerUnit}）");
        return true;
    }

    static bool TryBuildTargetSpritesBySuffix(
        string targetAutotilePath,
        out Dictionary<string, Sprite> targetBySuffix,
        out string error)
    {
        error = null;
        targetBySuffix = new Dictionary<string, Sprite>();

        var targetSprites = GetSpritesSortedByRect(targetAutotilePath);
        if (targetSprites.Count != ExpectedSpriteCount)
        {
            error = $"目标纹理子 Sprite 数量应为 {ExpectedSpriteCount}，实际 {targetSprites.Count}。";
            return false;
        }

        foreach (var targetSprite in targetSprites)
        {
            var suffix = GetSpriteSuffix(targetSprite.name);
            if (targetBySuffix.ContainsKey(suffix))
            {
                error = $"目标纹理存在重复后缀 {suffix}：{targetAutotilePath}";
                return false;
            }

            targetBySuffix[suffix] = targetSprite;
        }

        return true;
    }

    static RuleTile CloneRuleTile(
        RuleTile template,
        Texture2D referenceAutotile,
        Sprite fillSprite,
        Dictionary<string, Sprite> targetBySuffix,
        out string error)
    {
        error = null;

        var clone = UnityEngine.Object.Instantiate(template);
        clone.hideFlags = HideFlags.None;

        if (!TryMapSprite(template.m_DefaultSprite, referenceAutotile, fillSprite, targetBySuffix, out var defaultSprite, out error))
            return null;

        clone.m_DefaultSprite = defaultSprite;

        for (var i = 0; i < clone.m_TilingRules.Count; i++)
        {
            var rule = clone.m_TilingRules[i];
            if (rule.m_Sprites == null)
                continue;

            for (var s = 0; s < rule.m_Sprites.Length; s++)
            {
                if (!TryMapSprite(rule.m_Sprites[s], referenceAutotile, fillSprite, targetBySuffix, out var mapped, out error))
                    return null;

                rule.m_Sprites[s] = mapped;
            }
        }

        return clone;
    }

    static bool TryMapSprite(
        Sprite source,
        Texture2D referenceAutotile,
        Sprite fillSprite,
        Dictionary<string, Sprite> targetBySuffix,
        out Sprite mapped,
        out string error)
    {
        error = null;
        mapped = source;

        if (source == null)
            return true;

        if (source.texture == referenceAutotile)
        {
            var suffix = GetSpriteSuffix(source.name);
            if (targetBySuffix.TryGetValue(suffix, out mapped) && mapped != null)
                return true;

            error = $"找不到参考 Sprite 的映射：{source.name}（后缀 {suffix}）";
            return false;
        }

        mapped = fillSprite;
        if (mapped == null)
        {
            error = "填充 Sprite 为空。";
            return false;
        }

        return true;
    }

    static bool TryGetFillSprite(string fillTexturePath, out Sprite fillSprite, out string error)
    {
        error = null;
        fillSprite = null;

        if (string.IsNullOrEmpty(fillTexturePath))
        {
            error = "填充纹理路径为空。";
            return false;
        }

        var sprites = AssetDatabase.LoadAllAssetsAtPath(fillTexturePath)
            .OfType<Sprite>()
            .Where(s => s.rect.width > 0 && s.rect.height > 0)
            .ToList();

        if (sprites.Count == 0)
        {
            error = "填充纹理没有 Sprite：" + fillTexturePath;
            return false;
        }

        if (sprites.Count > 1)
            Debug.LogWarning($"[RuleTileGenerator] 填充纹理包含多个 Sprite，将使用第一个：{fillTexturePath}");

        fillSprite = sprites[0];
        return true;
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

    static string GetSpriteSuffix(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return "0";

        var index = spriteName.LastIndexOf('_');
        return index >= 0 && index < spriteName.Length - 1
            ? spriteName.Substring(index + 1)
            : spriteName;
    }
}
#endif
