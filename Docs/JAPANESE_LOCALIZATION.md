# Japanese localization workstream

Branch: `japanese-localization` (based on the upstream `main`).

## Current implementation

The translated in-game messages are in `Localization/ja.tsv` (6,874 entries:
including 404 object names, 213 spell and enchantment strings, 264 scenery
descriptions, 69 book/note entries, 115 inscriptions/signs and 4,908 dialogue
entries from 97 conversation blocks, plus Japanese status fragments, ordinal
labels, cutscene narration and internal diagnostics).
Set `UW_JA_TRANSLATIONS` to that file's full path to enable them,
or place `Localization/ja.tsv` next to the executable for automatic discovery.
When running from the Unity Editor the project root's `Localization/ja.tsv` is
loaded automatically as well. A post-build hook copies it into Windows and Linux
players, so no environment variable is needed for a normal fork build.
All 7,091 nonempty source strings are accounted for: 6,874 translated and 217
intentionally preserved in English or a fictional language. This is full review
coverage of the game's string archive, **not** a claim that every pixel of the
game UI is Japanese. Some original-image lettering requires separate handling.

## Build verification (2026-10-13)

- Unity 6000.6.0f1 imported and compiled all project scripts successfully.
- A Windows x64 player built successfully, and the post-build hook copied
  `Localization/ja.tsv` beside `UR.exe`.
- The translations were loaded successfully from the GOG installation
  by the engine-free `Tools/UWDump` tool, with and without the environment variable.
- `uwdump validate-tsv` checked 6,874 rows against the original strings: zero
  invalid keys, duplicate keys, newline differences, control-code or dialogue
  placeholder mismatches.
- The Editor smoke test (`UWJapaneseFontDiagnostics.VerifyJapaneseFont`) selected
  Noto Sans JP, found every tested Japanese glyph, generated 68 uGUI vertices,
  and verified Japanese/Latin per-label font switching. The test exited normally.
- `UWJapaneseFontDiagnostics.VerifyJapaneseClassicLabels` verified that the
  character-creation UI displays CJK with Noto Sans JP while untranslated
  Latin text still uses the original bitmap font in classic mode.
- The original `STRINGS.PAK` still supplies 217 deliberately unchanged nonempty
  strings and 3,893 empty entries; 122 string-block header records are also
  unchanged. The override validator compares each source entry and control code.
- The Windows player was launched with the GOG data and its character-creation
  screen visibly showed translated labels and options after the font fix.
  Visual evidence stays outside Git at `L:\Games\UlutimaUW\LocalizationPrivate\ja-fontfix-char-screen.png`.
  Text contrast against the original book background still needs improvement.
- Full UI layout, long text wrapping, remaining classic HUD paths, embedded
  English-language sprite text and gameplay are not yet fully tested. Subsequent
  preview builds use the previously verified executable with updated `ja.tsv`
  rather than recompiling the player each time.

The incremental translation counts (with empty source rows tracked separately) are
recorded in `Docs/TRANSLATION_PROGRESS.md`. The original source has 7,091
nonempty entries; 6,874 are translated, 217 intentionally retain source
tokens, and **zero** remain unreviewed. All 97 dialogue blocks
(3585-3612, 3648-3651, 3672-3674, 3696-3699, 3720-3728,
3730-3733, 3745-3746, 3768-3778, 3792-3806, 3815-3816,
3846-3847, 3852, 3856, 3860-3862, 3864-3866, 3872, 3875,
3879, 3881 and 3898) are now fully reviewed.

The intentional exceptions are enumerated in `Localization/ja-preserve.tsv`,
and the coverage script checks them against original nonempty block/index IDs.
These are **not** machine-translated or disguised as Japanese: lizard-speech
words, spoken passwords, shrine mantras, rune incantations, mine codes and
conversation lookup keywords must remain stable to preserve language puzzles
and script behaviour. Developer-report column headers, bare structural tokens
and a damaged inscription are also deliberately retained to protect formatting.

## Gameplay HUD follow-up

- Character creation is already showing Japanese correctly; do not change that screen
  when working on later HUD text adjustments.
- The classic in-game message log uses the original dark-brown palette colour. For
  ordinary Japanese lines, `UWHudMessageLog` uses light cream text for contrast;
  Japanese text with green/red game colour codes uses brighter pastel green/red.
  English messages retain their existing palette colours.
- The options save list labels unused slots `（未使用）` for display only. The original
  ASCII placeholder, editable save names and on-disk `DESC` data remain untouched.
- The left-side options buttons have English words painted into the original
  OPTBTNS.GR sprites, independently of STRINGS.PAK. UWJapaneseOptionsArt now
  replaces the palette-indexed Latin lettering and adjacent shadows at runtime,
  then places Japanese bold gothic uGUI text in the same original button bounds.
  Save, Restore, Music, Sound, Detail, Return, Quit, options state headings and
  confirmation buttons are covered. This also replaces the embedded text in
  the options backgrounds while preserving original borders, pressed-state art,
  Roman save-slot numbers, click rectangles, palette rendering and save data.
  If a Japanese OS font is unavailable, the unmodified English art is used.
  UWJapaneseFontDiagnostics.VerifyJapaneseOptionsButtons passed in Unity 6000.6.0f1;
  a new actual-game options screenshot is still required.
- The modern IMGUI setup bar and help window now use UWJapaneseInterface for
  the major navigation labels, options/dialog commands, explanatory hints,
  seven help tabs, the forty manual spell notes, mantra notes and many controls
  descriptions. Unknown or user-supplied text is left unchanged in English,
  and localization is disabled when no Japanese OS font is available.
- Unity 6000.6's IMGUI text layout crashed when a font generated solely with
  Font.CreateDynamicFontFromOSFont was assigned directly to a GUIStyle.
  The solution is to keep the game's imported Liberation Sans font with
  Include Font Data enabled, and configure Japanese OS font family names as
  fallback entries in its importer. It loads missing glyphs from the local
  machine without shipping extra fonts. The editor smoke check successfully
  fetched the Japanese glyph for 設, measured its advance and ran GUIStyle
  text-height calculations without crashing.
- UWJapaneseFontDiagnostics.VerifyJapaneseInterface now checks the OS font,
  uGUI glyph geometry, HUD colours, options artwork, localized navigation,
  forty help spell notes, mantra notes and IMGUI layout. All passed headlessly
  after the font fallback change. The real menu/help screen's appearance is
  still awaiting a visual in-game test.
- Follow-up work extends the separate modern-UI dictionary to 382 entries,
  including every binding label for keyboard/mouse and gamepad, conflict
  notifications, controller deadzone status, dynamic help keys and manual
  page navigation. The editor's AuditJapaneseControlsHelp creates both modern
  and classic controls guides with a temporary simulated gamepad. Its 117
  section headings, notes and action rows now have **zero** untranslated
  items; VerifyJapaneseInterface fails if this coverage regresses.
- The already-running player's older `Build-ja-hud` window could be captured
  without changing its state, but an isolated, hidden instance of the
  translated `Build-ja-6874-menus` renders black when captured through
  PrintWindow. Therefore no visual acceptance claim is made for the new
  menus: a normally visible player window must still be checked later.
- Later visual regression work succeeded at capturing the normally visible
  `Build-ja-6874-controls` top menu and all seven setup categories. The images
  exposed real problems which a headless translation audit did not catch:
  the Game dropdown's Japanese toggle labels exceeded its 260px bounds,
  the Motion modal stayed painted below the Graphics dropdown when switching
  tabs, and the UI retained English strings such as `At once`, `Orig.`,
  `(desktop)`, missing MT-32 ROM status, `Health` and `Poisoned`.
  Original screenshots remain private in `LocalizationPrivate\ja-tab-*.png`.
- `UWSetupMenu.fCloseTopBarPanels` now resets every top-level modal and dropdown
  when another top-bar category is chosen. The Japanese labels were shortened
  where they must fit the compact Game dropdown; status suffixes and missing
  MT-32 ROM messages are translated while preserving variable content.
  `UWJapaneseFontDiagnostics.VerifyExclusiveSetupPanels` asserts previous
  modal state is cleared, and the setup smoke test measures the three
  Japanese Game-menu toggle captions at the real 16px IMGUI font size:
  their measured widths are approximately 180, 180 and 161 pixels before
  reserving 24 pixels for the toggle icon (the available width is 244).
  Both tests pass in Unity 6000.6.0f1.
- The corrected build compiles and starts, and its title and character
  creation view can be captured separately. However, the second run's menu
  reveal has not yet been captured reliably after returning from character
  creation, so the *post-fix visual acceptance* remains outstanding. Do not
  claim the UI overlay is visually perfect solely from the headless tests.
- The Unity editor verified the Japanese/English colour selection, but the updated
  game message contrast still needs an in-game screenshot check.
- Japanese classic labels use Noto Sans JP with a **bold gothic style** in uGUI
  (English labels keep their normal bitmap font). The editor smoke test confirms
  this style as well as automatic wrapping of long Japanese text into CJK cells.
- Raw object-name articles and plurals are preserved in the translation files,
  but the object formatter removes the English articles when the name is Japanese.
  Japanese creature descriptions, enchantment suffixes, quantities and owners
  use localized word order in the updated game code.

When Unity Package Manager resolution is very slow on a non-system drive,
the same project sources can be copied to a temporary directory on a faster local
drive for an Editor build. Do not copy the generated `Library`, `Temp`, or `Logs`
directories: Unity regenerates them. Take care **not** to exclude source folders
called `Build`, such as `Assets/UWScripts/Build`. This does not change the original
project's `Packages/manifest.json` or `Packages/packages-lock.json`.

To export the original text privately (do not commit the resulting file):

```powershell
dotnet run --project Tools/UWDump -- export-tsv 'L:\Games\GOG\TocaRaceDriver3\Ultima Underworld' 'L:\Games\UlutimaUW\LocalizationPrivate\original-strings.tsv'
```

For a private reference picture of an original interface sprite, the data-only
tool can also export a portable PPM RGB image (or a PGM palette-index image)
without Unity. Keep these files outside the repository and never ship the
original game's images in a release:

```powershell
dotnet run --project Tools/UWDump -- export-gr 'L:\Games\GOG\TocaRaceDriver3\Ultima Underworld' OPTBTNS 6 'L:\Games\UlutimaUW\LocalizationPrivate\save-button.ppm'
```

To validate overrides after every translation batch:

```powershell
dotnet run --project Tools/UWDump -- validate-tsv 'L:\Games\GOG\TocaRaceDriver3\Ultima Underworld' 'Localization\ja.tsv'
```

`STRINGS.PAK` is decoded as before. The optional `UW_JA_TRANSLATIONS`
environment variable points to a UTF-8 TSV file with three columns:

```
# block<TAB>index<TAB>translation
1	3	鍵が合わない。\n
```

The example is a verified entry for block 1/index 3.
Only nonzero, existing string indices are replaced; all unspecified strings retain
their original wording. `\\n` in the translation becomes a newline. This leaves the
original game files untouched and makes upstream synchronization easier.

Example on Windows PowerShell, before launching the game in the same shell:

```powershell
$env:UW_JA_TRANSLATIONS = 'C:\Translations\underworld-ja.tsv'
```

## Outstanding work

### 2026-10-13: code-composed English/Japanese messages and name entry

An in-game screenshot showed `You read the 巻物...` directly before a correctly
translated scroll. The text was **not** an untranslated STRINGS.PAK entry:
`Interaction.fReadHeader` had an English-only prefix and suffix in code.
`UWObjectDescriptionFormatter.FormatReadMessage` now uses Japanese sentence
order (`巻物にはこう書かれていた……`) whenever the Japanese overrides are active;
without those overrides the original `You read the scroll...` remains unchanged.

Other code-composed strings were also checked. Named talismans use Japanese
look-message word order, conversation `identify_inv` strips the Japanese
`が見える。` sentence suffix, full bags have a Japanese message, and the health/
mana flasks show `current／maximum` instead of a mixed-language `out of`.
Equipment wear and destruction messages have Japanese wording when localized.

During character creation, the classic bitmap font cannot measure the width
of the translated `名前：` prompt (it reports zero). The typed name now starts
no farther left than the reserved input column, preventing overlap. Untranslated
English prompts keep the original width-based positioning.

The engine-free self-check explicitly verifies Japanese read messages and the
unchanged English fallback. The older game-rule checks contain assertions for
the English original strings, so a full original-game self-check is run with
the optional Japanese override disabled; the new Japanese composition tests run
in both modes. A fresh Windows player build includes these changes. A manual
in-game screenshot is still required to confirm the final visual positioning.

### 2026-10-13: real-game Japanese options label visibility regression

The previous `Build-ja-6874-visualfix` player starts successfully and supports
character creation and Japanese gameplay messages, but a real screenshot revealed
that the Japanese Save label (and other options captions) could remain visible
over the original left-side command icons **while the options menu was closed**.
The cause was `UnityEngine.UI.Image.enabled = false` disabling the painted
image but not the child `UnityEngine.UI.Text` object placed there for Japanese.
`UWJapaneseOptionsArt.SetVisible` now synchronizes parent image visibility
and child caption visibility. `ApplyLabel` clears obsolete text when a
sprite is switched to a nontranslated index and never resurrects a label on
a hidden image. The options menu now calls `SetVisible` on its background
and each menu entry. Editor regression checks assert hide/show, unchanged
Roman save slots, no stale Quit text after switching to the main options
screen, and the existing border/palette preservation tests. All interface
tests passed headlessly in Unity 6000.6.0f1 (exit code 0).

The new Windows executable was built and launched separately in
`Build-ja-6874-options-fixed`. A fresh test character was created and the
real gameplay image `LocalizationPrivate/options-fixed-after-name.png`
**confirmed no Japanese captions over the closed left-side command icons**.
Screenshots are private test artifacts, not bundled. This is a visual
acceptance test of the specific label-leak regression, not yet a complete
visual review of every open options submenu or of all translated dialogue.
Another gameplay test had shown residual English `out of` in some
health/mana status strings; the code-composed text fix above addresses it.


The modern HUD tries an installed OS Japanese font (Noto Sans CJK JP,
Noto Sans JP, Yu Gothic, Meiryo or MS Gothic) when the text contains Japanese.
The Noto Sans JP and uGUI selection paths passed the Unity Editor smoke test.
`UWTextLabel` (the original-style game's bitmap-font wrapper) also uses the
shared Japanese OS font whenever its text contains Japanese, even if the player
selected the classic font. This prevents invisible CJK strings in character
creation and other classic-style screens. Latin-only strings still render with
the original bitmap font when classic mode is selected.
For Linux, install Noto Sans CJK JP;
no operating-system fonts are bundled with the project.

- Export original string block/index inventory locally from a **legally installed**
  GOG game using Unity's `UW/Export All Strings` editor command; do not commit the
  extracted original text to this repository.
- Translate and verify all game messages, object names, state descriptions,
  books/scrolls, conversation blocks and cutscene captions.
- Locate interface labels and messages embedded in code or original `UW.EXE`.
- Add Japanese-capable font assets and verify rendering for the modern and classic
  UIs; the original bitmap fonts cannot be assumed to contain Japanese glyphs.
- Test placeholders, formatting, line wrapping, gamepad entry and save compatibility.

Note: this is a localization *foundation*, not yet a complete Japanese-language build.
