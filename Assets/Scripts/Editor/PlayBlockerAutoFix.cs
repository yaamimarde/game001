#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
static class PlayBlockerAutoFix
{
    const string SessionKey = "game001_play_blocker_fix_v10";
    const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string PlayerControllerPath = "Assets/Animations/Player/Player.controller";

    static PlayBlockerAutoFix()
    {
        EditorApplication.delayCall += () => RunOnce();
    }

    [MenuItem("Tools/Player/Fix Play Blockers")]
    public static void RunFromMenu()
    {
        SessionState.SetBool(SessionKey, false);
        RunOnce(force: true);
    }

    static bool CanRunEditorMaintenance()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    static void RunOnce(bool force = false)
    {
        if (!CanRunEditorMaintenance())
            return;

        if (!force && SessionState.GetBool(SessionKey, false))
            return;

        if (!force && !NeedsMaintenance())
        {
            SessionState.SetBool(SessionKey, true);
            return;
        }

        var dirty = false;

        if (PlayerNeedsAnimationRebuild())
        {
            PlayerSpriteAnimationBuilder.RebuildAll();
            dirty = true;
        }

        var fontAsset = ChineseUIFontBuilder.EnsureChineseUIFont();
        if (MainMenuNeedsRebuild())
        {
            GameSetupEditor.CreateMainMenuScene();
            dirty = true;
        }
        else if (fontAsset != null && FixMainMenuFonts(fontAsset))
        {
            dirty = true;
        }

        if (MainMenuNeedsRebuild())
        {
            Debug.LogWarning("[PlayBlockerAutoFix] MainMenu 场景尚未更新，请执行 Game → Setup → Rebuild Main Menu Scene。");
            return;
        }

        SessionState.SetBool(SessionKey, true);
        if (dirty)
            AssetDatabase.SaveAssets();

        Debug.Log("[PlayBlockerAutoFix] Editor maintenance complete.");
    }

    static bool NeedsMaintenance()
    {
        return PlayerNeedsAnimationRebuild() || MainMenuNeedsRebuild();
    }

    static bool PlayerNeedsAnimationRebuild()
    {
        return !File.Exists(PlayerControllerPath);
    }

    static bool MainMenuNeedsRebuild()
    {
        if (!File.Exists(MainMenuScenePath))
            return true;

        var text = File.ReadAllText(MainMenuScenePath);
        if (new FileInfo(MainMenuScenePath).Length < 256)
            return true;

        if (text.Contains("m_fontAsset: {fileID: 0}"))
            return true;

        if (!text.Contains("m_Name: PanelTitle") || !text.Contains("m_Name: NameRow"))
            return true;

        if (text.Contains("m_AnchoredPosition: {x: 0, y: -330}"))
            return true;

        return false;
    }

    static bool FixMainMenuFonts(TMP_FontAsset fontAsset)
    {
        if (!CanRunEditorMaintenance() || !File.Exists(MainMenuScenePath))
            return false;

        var activeScenePath = SceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

        var changed = false;
        foreach (var text in Object.FindObjectsOfType<TMP_Text>(true))
        {
            if (text.font != null)
                continue;

            text.font = fontAsset;
            EditorUtility.SetDirty(text);
            changed = true;
        }

        if (changed)
            EditorSceneManager.SaveScene(scene);

        if (!string.IsNullOrEmpty(activeScenePath) && activeScenePath != MainMenuScenePath)
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);

        return changed;
    }
}
#endif
