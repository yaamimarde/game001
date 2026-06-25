#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class PlayerSpriteAnimationBuilder
{
    const string PlaceholderRoot = "Assets/material/Player/Placeholder";
    const string AnimRoot = "Assets/Animations/Player";
    const string ControllerPath = AnimRoot + "/Player.controller";
    const string PrefabPath = AnimRoot + "/Player.prefab";

    public const int FrameWidth = 48;
    public const int FrameHeight = 64;
    public const int FrameCount = 6;
    public const int PixelsPerUnit = 100;
    public const float SampleRate = 12f;

    static readonly string[] Actions = { "Idle", "Walk", "Run", "Dash", "Jump", "Attack" };

    static readonly (string suffix, Vector2 blendPos)[] Directions =
    {
        ("down", new Vector2(0f, -1f)),
        ("up", new Vector2(0f, 1f)),
        ("left", new Vector2(-1f, 0f)),
        ("right", new Vector2(1f, 0f)),
        ("left_up", new Vector2(-0.707f, 0.707f)),
        ("right_up", new Vector2(0.707f, 0.707f)),
    };

    [MenuItem("Tools/Player/Generate Placeholder Sprites")]
    public static void GeneratePlaceholderSpritesMenu()
    {
        GeneratePlaceholderSprites();
        AssetDatabase.Refresh();
        Debug.Log("Placeholder sprites generated under " + PlaceholderRoot);
    }

    [MenuItem("Tools/Player/Rebuild Animation Clips")]
    public static void RebuildAnimationClipsMenu()
    {
        RebuildAll();
    }

    public static void GeneratePlaceholderSprites()
    {
        var colors = new Dictionary<string, Color>
        {
            { "down", new Color(0.24f, 0.47f, 0.86f, 1f) },
            { "up", new Color(0.24f, 0.78f, 0.35f, 1f) },
            { "left", new Color(0.94f, 0.55f, 0.2f, 1f) },
            { "right", new Color(0.67f, 0.31f, 0.86f, 1f) },
            { "left_up", new Color(0.16f, 0.82f, 0.82f, 1f) },
            { "right_up", new Color(0.94f, 0.86f, 0.24f, 1f) },
        };

        foreach (var action in Actions)
        {
            var dir = Path.Combine(PlaceholderRoot, action);
            Directory.CreateDirectory(dir);

            foreach (var (suffix, _) in Directions)
            {
                var tex = new Texture2D(FrameWidth * FrameCount, FrameHeight, TextureFormat.RGBA32, false);
                var pixels = new Color[tex.width * tex.height];
                var baseColor = colors[suffix];

                for (var frame = 0; frame < FrameCount; frame++)
                {
                    var shade = 0.15f + frame * 0.12f;
                    var c = baseColor * (0.7f + shade);
                    c.a = 1f;

                    for (var y = 8; y < FrameHeight - 8; y++)
                    {
                        for (var x = frame * FrameWidth + 4; x < (frame + 1) * FrameWidth - 4; x++)
                            pixels[y * tex.width + x] = c;
                    }
                }

                tex.SetPixels(pixels);
                tex.Apply();

                var prefix = action.ToLowerInvariant();
                var path = Path.Combine(dir, $"{prefix}_{suffix}.png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
        }
    }

    public static void RebuildAll()
    {
        SliceAllTextures();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var clipMap = CreateAllAnimationClips();
        RebuildAnimatorController(clipMap);
        UpdatePlayerPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Player animation rebuild complete.");
    }

    static void SliceAllTextures()
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();

        foreach (var action in Actions)
        {
            foreach (var (suffix, _) in Directions)
            {
                var prefix = action.ToLowerInvariant();
                var assetPath = $"{PlaceholderRoot}/{action}/{prefix}_{suffix}.png";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("Missing texture: " + assetPath);
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.spritePixelsPerUnit = PixelsPerUnit;

                var spriteRects = new SpriteRect[FrameCount];
                for (var i = 0; i < FrameCount; i++)
                {
                    spriteRects[i] = new SpriteRect
                    {
                        name = $"{prefix}_{suffix}_{i}",
                        rect = new Rect(i * FrameWidth, 0, FrameWidth, FrameHeight),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    };
                }

                var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
                if (provider == null)
                {
                    Debug.LogWarning("No sprite data provider for " + assetPath);
                    continue;
                }

                provider.InitSpriteEditorDataProvider();
                provider.SetSpriteRects(spriteRects);
                provider.Apply();

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }
    }

    static Dictionary<string, AnimationClip> CreateAllAnimationClips()
    {
        var map = new Dictionary<string, AnimationClip>();

        foreach (var action in Actions)
        {
            foreach (var (suffix, _) in Directions)
            {
                var prefix = action.ToLowerInvariant();
                var clipName = $"Player_{action}_{SuffixToPascal(suffix)}";
                var clipPath = $"{AnimRoot}/{clipName}.anim";
                var sprites = LoadSprites(prefix, suffix);
                if (sprites.Count == 0)
                {
                    Debug.LogWarning("No sprites for " + clipName);
                    continue;
                }

                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (existing != null)
                    AssetDatabase.DeleteAsset(clipPath);

                var clip = CreateSpriteClip(clipName, sprites, loop: action is "Idle" or "Walk" or "Run");
                AssetDatabase.CreateAsset(clip, clipPath);
                map[clipName] = clip;
            }
        }

        return map;
    }

    static List<Sprite> LoadSprites(string prefix, string suffix)
    {
        var assetPath = $"{PlaceholderRoot}/{PrefixToActionFolder(prefix)}/{prefix}_{suffix}.png";
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToList();
    }

    static string PrefixToActionFolder(string prefix) =>
        char.ToUpper(prefix[0]) + prefix.Substring(1);

    static AnimationClip CreateSpriteClip(string clipName, List<Sprite> sprites, bool loop)
    {
        var clip = new AnimationClip { frameRate = SampleRate };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Count];
        var frameTime = 1f / SampleRate;

        for (var i = 0; i < sprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i * frameTime,
                value = sprites[i],
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.name = clipName;
        return clip;
    }

    static void RebuildAnimatorController(Dictionary<string, AnimationClip> clipMap)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack1", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack3", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;
        var idle = CreateBlendState(controller, sm, "Idle", clipMap, "Idle", new Vector3(60, 50, 0));
        var walk = CreateBlendState(controller, sm, "Walk_Directional", clipMap, "Walk", new Vector3(300, 50, 0));
        var run = CreateBlendState(controller, sm, "Run_Directional", clipMap, "Run", new Vector3(300, 150, 0), 1.5f);
        var dash = CreateBlendState(controller, sm, "Dash_Directional", clipMap, "Dash", new Vector3(50, 250, 0));
        var jump = CreateBlendState(controller, sm, "Jump_Directional", clipMap, "Jump", new Vector3(300, 250, 0));
        var attack = CreateBlendState(controller, sm, "Attack_Directional", clipMap, "Attack", new Vector3(550, 150, 0));

        sm.defaultState = idle;

        AddTransition(idle, walk,
            (AnimatorConditionMode.Greater, "Speed", 0.1f),
            (AnimatorConditionMode.IfNot, "IsRunning", 0f),
            (AnimatorConditionMode.IfNot, "IsDashing", 0f));
        AddTransition(idle, run,
            (AnimatorConditionMode.Greater, "Speed", 0.1f),
            (AnimatorConditionMode.If, "IsRunning", 0f),
            (AnimatorConditionMode.IfNot, "IsDashing", 0f));
        AddTransition(walk, idle,
            (AnimatorConditionMode.Less, "Speed", 0.1f),
            (AnimatorConditionMode.IfNot, "IsDashing", 0f));
        AddTransition(walk, run,
            (AnimatorConditionMode.If, "IsRunning", 0f),
            (AnimatorConditionMode.IfNot, "IsDashing", 0f));
        AddTransition(run, walk,
            (AnimatorConditionMode.IfNot, "IsRunning", 0f),
            (AnimatorConditionMode.IfNot, "IsDashing", 0f));
        AddTransition(run, idle,
            (AnimatorConditionMode.Less, "Speed", 0.1f),
            (AnimatorConditionMode.IfNot, "IsDashing", 0f));

        AddAnyStateTransition(sm, dash, "IsDashing", true, 0f, false);
        AddTransition(dash, idle, (AnimatorConditionMode.IfNot, "IsDashing", 0f));

        AddAnyStateTransition(sm, jump, "IsJumping", true, 0f, false);
        AddTransition(jump, idle, (AnimatorConditionMode.IfNot, "IsJumping", 0f));

        AddAnyStateTriggerTransition(sm, attack, "Attack1");
        AddAnyStateTriggerTransition(sm, attack, "Attack2");
        AddAnyStateTriggerTransition(sm, attack, "Attack3");

        var attackExit = attack.AddTransition(idle);
        attackExit.hasExitTime = true;
        attackExit.exitTime = 0.85f;
        attackExit.duration = 0.05f;

        EditorUtility.SetDirty(controller);
    }

    static AnimatorState CreateBlendState(
        AnimatorController controller,
        AnimatorStateMachine sm,
        string stateName,
        Dictionary<string, AnimationClip> clipMap,
        string action,
        Vector3 pos,
        float speed = 1f)
    {
        var state = sm.AddState(stateName, pos);
        state.speed = speed;
        state.motion = BuildDirectionalBlendTree(controller, clipMap, action, stateName + "_Blend");
        return state;
    }

    static BlendTree BuildDirectionalBlendTree(
        AnimatorController controller,
        Dictionary<string, AnimationClip> clipMap,
        string action,
        string treeName)
    {
        var tree = new BlendTree
        {
            name = treeName,
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            useAutomaticThresholds = false,
        };

        foreach (var (suffix, blendPos) in Directions)
        {
            var clipName = $"Player_{action}_{SuffixToPascal(suffix)}";
            if (clipMap.TryGetValue(clipName, out var clip))
                tree.AddChild(clip, blendPos);
        }

        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    static void AddTransition(AnimatorState from, AnimatorState to, params (AnimatorConditionMode mode, string param, float threshold)[] conditions)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.05f;
        foreach (var c in conditions)
            t.AddCondition(c.mode, c.threshold, c.param);
    }

    static void AddAnyStateTransition(
        AnimatorStateMachine sm,
        AnimatorState dst,
        string param,
        bool value,
        float duration,
        bool canTransitionToSelf)
    {
        var t = sm.AddAnyStateTransition(dst);
        t.hasExitTime = false;
        t.duration = duration;
        t.canTransitionToSelf = canTransitionToSelf;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
    }

    static void AddAnyStateTriggerTransition(AnimatorStateMachine sm, AnimatorState dst, string trigger)
    {
        var t = sm.AddAnyStateTransition(dst);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    static void UpdatePlayerPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var sr = root.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var sprites = LoadSprites("idle", "down");
            if (sprites.Count > 0)
                sr.sprite = sprites[0];
        }

        root.transform.localScale = Vector3.one;
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static string SuffixToPascal(string suffix) =>
        string.Concat(suffix.Split('_').Select(part =>
            char.ToUpperInvariant(part[0]) + part.Substring(1)));
}
#endif
