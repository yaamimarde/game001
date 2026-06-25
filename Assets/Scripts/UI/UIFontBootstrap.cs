using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class UIFontBootstrap : MonoBehaviour
{
    void Awake()
    {
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
            return;

        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
        {
            if (text.font == null)
                text.font = defaultFont;
        }
    }
}
