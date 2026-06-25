#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class MainMenuUIFontAutoFix
{
    const string SessionKey = "game001_mainmenu_ui_font_fix_v8";
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    static MainMenuUIFontAutoFix()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SceneNeedsRebuild() && SessionState.GetBool(SessionKey, false))
                return;

            if (SceneNeedsRebuild())
                RunOnce(force: true);
        };
    }

    static bool CanRunEditorMaintenance()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("Game/Setup/Fix Main Menu Chinese UI")]
    public static void RunFromMenu()
    {
        SessionState.SetBool(SessionKey, false);
        RunOnce(force: true);
    }

    static void RunOnce(bool force = false)
    {
        if (!CanRunEditorMaintenance())
            return;

        if (!force && SessionState.GetBool(SessionKey, false))
            return;

        if (!force && !SceneNeedsRebuild())
        {
            SessionState.SetBool(SessionKey, true);
            return;
        }

        if (ChineseUIFontBuilder.EnsureChineseUIFont() == null)
            return;

        if (force || SceneNeedsRebuild())
            GameSetupEditor.CreateMainMenuScene();

        if (SceneNeedsRebuild())
        {
            Debug.LogWarning("[MainMenuUIFontAutoFix] MainMenu 场景尚未更新，请执行 Game → Setup → Rebuild Main Menu Scene。");
            return;
        }

        SessionState.SetBool(SessionKey, true);
        Debug.Log("[MainMenuUIFontAutoFix] MainMenu scene verified.");
    }

    static bool SceneNeedsRebuild()
    {
        if (!File.Exists(MainMenuPath))
            return true;

        var info = new FileInfo(MainMenuPath);
        if (info.Length < 256)
            return true;

        var text = File.ReadAllText(MainMenuPath);
        if (text.Contains("m_fontAsset: {fileID: 0}"))
            return true;

        if (!text.Contains("m_Name: PanelTitle") || !text.Contains("m_Name: NameRow"))
            return true;

        if (text.Contains("m_AnchoredPosition: {x: 0, y: -330}"))
            return true;

        return false;
    }
}
#endif
