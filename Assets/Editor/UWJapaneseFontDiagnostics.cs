using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UWDataImport.UWData;

/// <summary>Headless smoke test of the OS-font path used by the Japanese translation.</summary>
public static class UWJapaneseFontDiagnostics
{
    [MenuItem("UW/Diagnostics/Verify Japanese Font")]
    public static void VerifyJapaneseFont()
    {
        string[] installed = Font.GetOSInstalledFontNames();
        var possible = new List<string>();
        foreach (string family in installed)
        {
            if (family.IndexOf("Noto", StringComparison.OrdinalIgnoreCase) >= 0 ||
                family.IndexOf("Gothic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                family.IndexOf("Meiryo", StringComparison.OrdinalIgnoreCase) >= 0)
                possible.Add(family);
        }
        Debug.Log("JP_FONT_AVAILABLE_FAMILIES=" + string.Join(", ", possible.ToArray()));

        MethodInfo select = typeof(UWUiFonts).GetMethod("fJapaneseFont", BindingFlags.NonPublic | BindingFlags.Static);
        if (select == null)
            throw new InvalidOperationException("JP_FONT_FAIL: Missing Japanese font selection method");
        Font selected = (Font)select.Invoke(null, new object[] { "鍵が合わない。日本語テスト" });
        if (selected == null)
            throw new InvalidOperationException("JP_FONT_FAIL: No usable Japanese font was selected");

        string sample = "鍵が合わない。ひらがなカタカナ漢字";
        var missing = new List<char>();
        foreach (char c in sample)
        {
            if (!selected.HasCharacter(c))
                missing.Add(c);
        }

        Debug.Log("JP_FONT_SELECTED=" + selected.name + " missing=" + string.Join("", missing.ConvertAll(c => c.ToString()).ToArray()));
        if (missing.Count > 0)
            throw new InvalidOperationException("JP_FONT_FAIL: Missing Japanese glyphs");

        // Exercise the real legacy uGUI Text registration and per-text font switching.
        var host = new GameObject("Japanese text smoke test", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        try
        {
            Text label = host.GetComponent<Text>();
            Font western = Font.CreateDynamicFontFromOSFont("Arial", 18);
            if (western == null)
                throw new InvalidOperationException("JP_FONT_FAIL: Missing test fallback font");
            label.font = western;
            label.fontSize = 24;
            label.text = sample;
            UWUiFonts.Register(label, null);
            UWUiFonts.ApplyNow(label);
            if (label.font != selected)
                throw new InvalidOperationException("JP_FONT_FAIL: uGUI did not select the Japanese font");

            TextGenerationSettings settings = label.GetGenerationSettings(new Vector2(900, 180));
            var generator = new TextGenerator();
            if (!generator.PopulateWithErrors(sample, settings, null) || generator.vertexCount < 8)
                throw new InvalidOperationException("JP_FONT_FAIL: uGUI did not produce Japanese text vertices");
            Debug.Log("JP_FONT_UGUI_VERTICES=" + generator.vertexCount);

            label.text = "English only";
            UWUiFonts.ApplyNow(label);
            if (label.font != western)
                throw new InvalidOperationException("JP_FONT_FAIL: Latin text did not revert to the selected UI face");

            Debug.Log("JP_FONT_UI_OK: Switching and uGUI geometry passed");
            UnityEngine.Object.DestroyImmediate(western);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
        Debug.Log("JP_FONT_OK: CJK glyph coverage smoke test passed");
    }

    [MenuItem("UW/Diagnostics/Verify Japanese Classic Labels")]
    public static void VerifyJapaneseClassicLabels()
    {
        string data = Environment.GetEnvironmentVariable("UW_JA_TEST_DATA");
        if (string.IsNullOrEmpty(data) || !File.Exists(Path.Combine(data, "FONTCHAR.SYS")))
            throw new InvalidOperationException("JP_CLASSIC_FAIL: UW_JA_TEST_DATA must name the extracted GOG DATA directory");

        var font = new UWFont(Path.Combine(data, "FONTCHAR.SYS"));
        if (!font.IsLoaded)
            throw new InvalidOperationException("JP_CLASSIC_FAIL: Original character-creation font not loaded");

        bool previous = UWTextLabel.UseModernFont;
        var canvas = new GameObject("Japanese classic label test", typeof(RectTransform), typeof(Canvas));
        try
        {
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var labelObject = new GameObject("Character creation question", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
            labelObject.transform.SetParent(canvas.transform, false);
            RawImage image = labelObject.GetComponent<RawImage>();
            var colour = new Color32(255, 220, 150, 255);

            UWTextLabel.UseModernFont = false;
            UWScreenUi.SetText(image, font, "性別を選択：", colour, 140, true);
            Text rendered = labelObject.GetComponentInChildren<Text>(true);
            Font japanese = UWUiFonts.JapaneseFontFor("性別を選択：");
            if (rendered == null || !rendered.enabled || image.enabled || japanese == null || rendered.font != japanese)
                throw new InvalidOperationException("JP_CLASSIC_FAIL: Japanese labels use an invisible bitmap texture");
            if (new TextGenerator().GetPreferredWidth("性別を選択：",
                    rendered.GetGenerationSettings(new Vector2(140, 100))) <= 0)
                throw new InvalidOperationException("JP_CLASSIC_FAIL: Japanese label has no visible width");
            Debug.Log("JP_CLASSIC_JAPANESE_OK: Original mode now uses " + rendered.font.name);

            // A Latin-only label must still use the original pixels in classic mode.
            UWScreenUi.SetText(image, font, "Male", colour, 67, true);
            if (!image.enabled || rendered.enabled)
                throw new InvalidOperationException("JP_CLASSIC_FAIL: English text did not retain original bitmap font");
            Debug.Log("JP_CLASSIC_ENGLISH_OK: Untranslated labels remain in original font");
        }
        finally
        {
            UWTextLabel.UseModernFont = previous;
            UnityEngine.Object.DestroyImmediate(canvas);
        }
    }
}
