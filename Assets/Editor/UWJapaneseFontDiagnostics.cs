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
            if (rendered.fontStyle != FontStyle.Bold)
                throw new InvalidOperationException("JP_CLASSIC_FAIL: Japanese labels did not use bold gothic");
            if (new TextGenerator().GetPreferredWidth("性別を選択：",
                    rendered.GetGenerationSettings(new Vector2(140, 100))) <= 0)
                throw new InvalidOperationException("JP_CLASSIC_FAIL: Japanese label has no visible width");
            Debug.Log("JP_CLASSIC_JAPANESE_OK: Original mode now uses " + rendered.font.name);

            // A Latin-only label must still use the original pixels in classic mode.
            UWScreenUi.SetText(image, font, "Male", colour, 67, true);
            if (!image.enabled || rendered.enabled)
                throw new InvalidOperationException("JP_CLASSIC_FAIL: English text did not retain original bitmap font");
            if (rendered.fontStyle != FontStyle.Normal)
                throw new InvalidOperationException("JP_CLASSIC_FAIL: English label style was changed");
            Debug.Log("JP_CLASSIC_ENGLISH_OK: Untranslated labels remain in original font");

            var lines = UWTextLayout.WrapText(font,
                "これは長い日本語の文章であり、迷宮の壁と罠の説明が表示領域を超えないよう折り返す必要がある。", 65);
            if (lines.Count < 3 || lines.Exists(line => line.Length > 12))
                throw new InvalidOperationException("JP_CLASSIC_FAIL: Japanese line wrapping did not fit the panel");
            Debug.Log("JP_CLASSIC_WRAP_OK: Japanese example wrapped into " + lines.Count + " lines");
        }
        finally
        {
            UWTextLabel.UseModernFont = previous;
            UnityEngine.Object.DestroyImmediate(canvas);
        }
    }

    [MenuItem("UW/Diagnostics/Verify Japanese HUD Colours")]
    public static void VerifyJapaneseHudColours()
    {
        Color32 normal = new Color32(60, 40, 32, 255);
        Color32 red = new Color32(220, 40, 35, 255);
        Color32 green = new Color32(70, 125, 60, 255);
        Color32 japanese = UWHudMessageLog.DisplayColourForLine("何も見えない。", normal, normal, green, red);

        if (japanese.r < 230 || japanese.g < 200 || japanese.b < 175)
            throw new InvalidOperationException("JP_HUD_FAIL: Japanese log text is too dark");
        if (!UWHudMessageLog.DisplayColourForLine("Nothing is visible.", normal, normal, green, red).Equals(normal))
            throw new InvalidOperationException("JP_HUD_FAIL: Original English ink changed");
        Color32 warning = UWHudMessageLog.DisplayColourForLine("失敗した。", red, normal, green, red);
        Color32 success = UWHudMessageLog.DisplayColourForLine("セーブデータ一覧", green, normal, green, red);
        if (warning.r < 240 || warning.g < 150 || warning.g > warning.r)
            throw new InvalidOperationException("JP_HUD_FAIL: Japanese warnings are not bright red");
        if (success.r < 180 || success.g < 230 || success.g <= success.r)
            throw new InvalidOperationException("JP_HUD_FAIL: Japanese success text is not light green");
        if (!UWHudMessageLog.DisplayColourForLine("Saved game.", green, normal, green, red).Equals(green))
            throw new InvalidOperationException("JP_HUD_FAIL: English success ink changed");

        Debug.Log("JP_HUD_COLOURS_OK: Japanese uses light ink; English and special colours preserved");
    }

    [MenuItem("UW/Diagnostics/Verify Japanese Options Buttons")]
    public static void VerifyJapaneseOptionsButtons()
    {
        Font japanese = UWUiFonts.JapaneseFontFor("保存　終了");
        if (japanese == null)
            throw new InvalidOperationException("JP_OPTIONS_FAIL: no installed Japanese gothic font");
        if (UWJapaneseOptionsArt.LabelFor(6) != "セーブ" ||
            UWJapaneseOptionsArt.LabelFor(7) != "セーブ" ||
            UWJapaneseOptionsArt.LabelFor(30) != null ||
            !UWJapaneseOptionsArt.NeedsCleaning(3) ||
            UWJapaneseOptionsArt.NeedsCleaning(30))
            throw new InvalidOperationException("JP_OPTIONS_FAIL: incorrect sprite/label mapping");

        const int width = 31, height = 14;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(35, 0, 0, 255);
        for (int x = 0; x < width; x++) pixels[x] = new Color32(1, 0, 0, 255); // border
        pixels[5 * width + 13] = new Color32(98, 0, 0, 255); // normal lettering
        pixels[5 * width + 14] = new Color32(1, 0, 0, 255); // adjacent shadow
        pixels[6 * width + 13] = new Color32(96, 0, 0, 255); // pressed lettering
        pixels[6 * width + 14] = new Color32(146, 0, 0, 255); // variant
        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        var canvas = new GameObject("Japanese options test", typeof(RectTransform), typeof(Canvas));
        try
        {
            UWJapaneseOptionsArt.EraseLatinInk(texture);
            var cleaned = texture.GetPixels32();
            for (int y = 5; y <= 6; y++)
            for (int x = 13; x <= 14; x++)
                if (cleaned[y * width + x].r != 35)
                    throw new InvalidOperationException("JP_OPTIONS_FAIL: Latin ink or shadow remains");
            if (cleaned[13].r != 1)
                throw new InvalidOperationException("JP_OPTIONS_FAIL: the original button border was damaged");

            GameObject node = new GameObject("Option button", typeof(RectTransform), typeof(Image));
            node.transform.SetParent(canvas.transform, false);
            Image image = node.GetComponent<Image>();
            image.sprite = Sprite.Create(texture, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), 1f);
            UWJapaneseOptionsArt.ApplyLabel(image, 6, japanese);
            Text label = node.GetComponentInChildren<Text>(true);
            if (label == null || label.text != "セーブ" || label.font != japanese ||
                label.fontStyle != FontStyle.Bold || label.raycastTarget)
                throw new InvalidOperationException("JP_OPTIONS_FAIL: Japanese button label is missing");
            if (label.fontSize < 6 || label.fontSize > 9)
                throw new InvalidOperationException("JP_OPTIONS_FAIL: Japanese button label does not fit");
            UWJapaneseOptionsArt.ApplyLabel(image, 7, japanese);
            if (node.GetComponentsInChildren<Text>(true).Length != 1)
                throw new InvalidOperationException("JP_OPTIONS_FAIL: selection created duplicate labels");
            UWJapaneseOptionsArt.ApplyLabel(image, 30, japanese);
            if (label.gameObject.activeSelf)
                throw new InvalidOperationException("JP_OPTIONS_FAIL: Roman save-slot numbers were replaced");
            Debug.Log("JP_OPTIONS_OK: labels, selection, original borders and ink removal verified");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(canvas);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    [MenuItem("UW/Diagnostics/Verify Japanese Setup And Help")]
    public static void VerifyJapaneseSetupAndHelp()
    {
        string[] bar = { "Game", "Motion", "Graphics", "Display", "Controls", "Sound", "Colours" };
        string[] expected = { "ゲーム", "移動", "画質", "表示", "操作", "音声", "色補正" };
        Font font = UWJapaneseInterface.FontOr(null);
        if (font == null || font != UWInterfaceFont.Font)
            throw new InvalidOperationException("JP_MENU_FAIL: IMGUI must use the imported Latin base font");
        Debug.Log("JP_MENU_FONT=" + font.name + " dynamic=" + font.dynamic);
        var importer = AssetImporter.GetAtPath("Assets/Resources/Fonts/LiberationSans-Regular.ttf")
            as TrueTypeFontImporter;
        if (importer == null || Array.IndexOf(importer.fontNames, "Noto Sans JP") < 0)
            throw new InvalidOperationException("JP_MENU_FAIL: OS Japanese font fallback is not configured");
        font.RequestCharactersInTexture("ゲーム設定", 16, FontStyle.Normal);
        bool hasJapanese = font.GetCharacterInfo('設', out CharacterInfo sampleCharacter, 16, FontStyle.Normal);
        Debug.Log("JP_MENU_FALLBACK_GLYPH=" + hasJapanese + " width=" + sampleCharacter.advance);

        for (int i = 0; i < bar.Length; i++)
            if (UWJapaneseInterface.Text(bar[i]) != expected[i])
                throw new InvalidOperationException("JP_MENU_FAIL: untranslated top menu " + bar[i]);

        foreach (string note in UWHelpContent.ManualSpellNotes.Values)
            if (UWJapaneseInterface.Text(note) == note)
                throw new InvalidOperationException("JP_MENU_FAIL: untranslated spell help note " + note);

        foreach (KeyValuePair<int, string> mantra in UWHelpContent.ManualMantras)
            if (UWJapaneseInterface.Text(mantra.Value) == mantra.Value)
                throw new InvalidOperationException("JP_MENU_FAIL: untranslated mantra note");

        foreach (UWKeyBindings.Entry binding in UWKeyBindings.Entries)
            if (UWJapaneseInterface.Text(binding.Label) == binding.Label)
                throw new InvalidOperationException("JP_MENU_FAIL: untranslated key binding " + binding.Label);
        foreach (UWKeyBindings.Entry binding in UWKeyBindings.PadEntries)
            if (UWJapaneseInterface.Text(binding.Label) == binding.Label)
                throw new InvalidOperationException("JP_MENU_FAIL: untranslated pad binding " + binding.Label);

        if (UWJapaneseInterface.Text("Strength") != "筋力" ||
            UWJapaneseInterface.Text("Colour strength") != "補正の強さ" ||
            UWJapaneseInterface.Text("At once") != "即時" ||
            UWJapaneseInterface.Text("Orig.") != "原作" ||
            UWJapaneseInterface.Text("Health") != "体力" ||
            UWJapaneseInterface.Text("Poisoned") != "毒状態" ||
            UWJapaneseInterface.Text("2560 x 1080   (desktop)") != "2560 x 1080   （デスクトップ）" ||
            !UWJapaneseInterface.Text("MT-32: no MT-32 or CM-32L ROMs in that folder").Contains("ROMがありません") ||
            UWJapaneseInterface.Text("Circle 4  -  12 mana") != "第4環 － 消費魔力12" ||
            UWJapaneseInterface.Text("Page 3 of 28") != "28ページ中 3ページ目" ||
            UWJapaneseInterface.Text("Pages 3 and 4 of 28") != "全28ページ、3・4ページ目" ||
            !UWJapaneseInterface.Text("Pages 3 and 4 of 28      Arrow keys or the wheel turn the pages, Esc or a click on the page closes").Contains("Esc") ||
            !UWJapaneseInterface.Text("This button is also used for \"Look\".").Contains("調べる") ||
            !UWJapaneseInterface.Text("The crosshair aims: E, Q and the left button act on what it is on.").Contains("Qキー") ||
            UWJapaneseInterface.Text("Homebrew custom text") != "Homebrew custom text")
            throw new InvalidOperationException("JP_MENU_FAIL: dynamic values or English fallback changed");

        GUIStyle style = new GUIStyle { font = font, fontSize = 14, wordWrap = true };
        foreach (string text in expected)
            if (style.CalcHeight(new GUIContent(text), 90f) <= 0)
                throw new InvalidOperationException("JP_MENU_FAIL: label cannot be measured " + text);

        // The compact top-left Game dropdown is only 260 reference pixels wide.
        // It has an 8-pixel inset and the toggle reserves room for its indicator.
        // Unity's GUI.skin cannot be read in a headless batchmode method (outside
        // OnGUI), so measure the font's text and reserve an additional 24 pixels
        // for the checkbox and its spacing instead.
        var toggleText = new GUIStyle { font = font, fontSize = 16 };
        string[] compactLabels = {
            "  Logos and intro at startup",
            "  Keep running in the background",
            "  Choose attributes at creation"
        };
        foreach (string english in compactLabels)
        {
            string translated = UWJapaneseInterface.Text(english);
            float needed = toggleText.CalcSize(new GUIContent(translated)).x;
            Debug.Log("JP_GAME_TOGGLE_WIDTH " + translated + " = " + needed + "px");
            if (needed + 24f > 244f)
                throw new InvalidOperationException("JP_MENU_FAIL: Game menu toggle clips (" +
                    needed + "px): " + translated);
        }

        Debug.Log("JP_MENU_HELP_OK: translated navigation, spell and mantra help, all binding labels, dynamic messages and font geometry");
    }

    [MenuItem("UW/Diagnostics/Verify Exclusive Setup Panels")]
    public static void VerifyExclusiveSetupPanels()
    {
        var test = new GameObject("Setup panel exclusivity test");
        try
        {
            UWSetupMenu menu = test.AddComponent<UWSetupMenu>();
            var type = typeof(UWSetupMenu);
            const System.Reflection.BindingFlags privateInstance =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            string[] flags = {
                "mbFolderDialogOpen", "mbControlsDialogOpen", "mbColoursDialogOpen",
                "mbSoundDialogOpen", "mbMotionDialogOpen", "mbDisplayDialogOpen",
                "mbPaletteEffectsDialogOpen"
            };

            foreach (string name in flags)
            {
                var field = type.GetField(name, privateInstance);
                if (field == null) throw new InvalidOperationException("JP_PANELS_FAIL: missing " + name);
                field.SetValue(menu, true);
            }
            var dropDown = type.GetField("meOpenMenu", privateInstance);
            if (dropDown == null) throw new InvalidOperationException("JP_PANELS_FAIL: no dropdown state");
            dropDown.SetValue(menu, Enum.Parse(dropDown.FieldType, "Graphics"));
            var clear = type.GetMethod("fCloseTopBarPanels", privateInstance);
            if (clear == null) throw new InvalidOperationException("JP_PANELS_FAIL: no exclusive switch");
            clear.Invoke(menu, null);

            foreach (string name in flags)
                if ((bool)type.GetField(name, privateInstance).GetValue(menu))
                    throw new InvalidOperationException("JP_PANELS_FAIL: stacked dialog " + name);
            if (dropDown.GetValue(menu).ToString() != "None")
                throw new InvalidOperationException("JP_PANELS_FAIL: old dropdown remains open");
            Debug.Log("JP_PANELS_OK: switching top-level tabs clears all prior dialogs and dropdowns");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(test);
        }
    }

    [MenuItem("UW/Diagnostics/Verify Japanese Interface")]
    public static void VerifyJapaneseInterface()
    {
        VerifyJapaneseFont();
        VerifyJapaneseHudColours();
        VerifyJapaneseOptionsButtons();
        VerifyJapaneseSetupAndHelp();
        VerifyExclusiveSetupPanels();
        AuditJapaneseControlsHelp();
        Debug.Log("JP_INTERFACE_ALL_OK: font, colours, classic options artwork, setup and help passed");
    }

    [MenuItem("UW/Diagnostics/Audit Japanese Controls Help")]
    public static void AuditJapaneseControlsHelp()
    {
        if (!UWJapaneseInterface.IsAvailable)
            throw new InvalidOperationException("JP_HELP_AUDIT_FAIL: no installed Japanese font");
        int total = 0, untranslated = 0;
        var simulatedPad = UnityEngine.InputSystem.Gamepad.current == null
            ? UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>() : null;
        try
        {
            foreach (bool modern in new[] { true, false })
            {
                foreach (UWHelpControls.Section section in UWHelpControls.Build(modern))
                {
                    string[] captions = { section.Title, section.Intro };
                    foreach (string caption in captions)
                    {
                        if (string.IsNullOrEmpty(caption)) continue;
                        total++;
                        if (UWJapaneseInterface.Text(caption) != caption) continue;
                        untranslated++;
                        Debug.Log("JP_HELP_UNTRANSLATED: " + caption);
                    }
                    foreach (UWHelpControls.Row row in section.Rows)
                    {
                        total++;
                        if (UWJapaneseInterface.Text(row.Text) != row.Text) continue;
                        untranslated++;
                        Debug.Log("JP_HELP_UNTRANSLATED: " + row.Text);
                    }
                }
            }
        }
        finally
        {
            if (simulatedPad != null)
                UnityEngine.InputSystem.InputSystem.RemoveDevice(simulatedPad);
        }
        Debug.Log("JP_HELP_AUDIT total=" + total + " untranslated=" + untranslated);
        if (untranslated != 0)
            throw new InvalidOperationException("JP_HELP_AUDIT_FAIL: " + untranslated +
                " actual controls-help lines remain untranslated");
    }
}
