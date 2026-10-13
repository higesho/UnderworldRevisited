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
