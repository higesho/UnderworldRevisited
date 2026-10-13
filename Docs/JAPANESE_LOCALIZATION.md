# Japanese localization workstream

Branch: `japanese-localization` (based on the upstream `main`).

## Current implementation

The translated in-game messages are in `Localization/ja.tsv` (5,786 entries:
including 404 object names, 213 spell and enchantment strings, 251 scenery
descriptions, 69 book/note entries, 115 inscriptions/signs and 4,368 dialogue
entries from 80 conversation blocks).
Set `UW_JA_TRANSLATIONS` to that file's full path to enable them,
or place `Localization/ja.tsv` next to the executable for automatic discovery.
When running from the Unity Editor the project root's `Localization/ja.tsv` is
loaded automatically as well. A post-build hook copies it into Windows and Linux
players, so no environment variable is needed for a normal fork build.
Translation coverage is incomplete; remaining strings fall back to English.

## Build verification (2026-10-13)

- Unity 6000.6.0f1 imported and compiled all project scripts successfully.
- A Windows x64 player built successfully, and the post-build hook copied
  `Localization/ja.tsv` beside `UR.exe`.
- The translations were loaded successfully from the GOG installation
  by the engine-free `Tools/UWDump` tool, with and without the environment variable.
- `uwdump validate-tsv` checked 5,786 rows against the original strings: zero
  invalid keys, duplicate keys, newline differences, control-code or dialogue
  placeholder mismatches.
- The Editor smoke test (`UWJapaneseFontDiagnostics.VerifyJapaneseFont`) selected
  Noto Sans JP, found every tested Japanese glyph, generated 68 uGUI vertices,
  and verified Japanese/Latin per-label font switching. The test exited normally.
- `UWJapaneseFontDiagnostics.VerifyJapaneseClassicLabels` verified that the
  character-creation UI displays CJK with Noto Sans JP while untranslated
  Latin text still uses the original bitmap font in classic mode.
- The original `STRINGS.PAK` remains the source for 5,320 untranslated entries,
  checked byte-for-byte against the decoded originals by `validate-tsv`.
- The Windows player was launched with the GOG data and its character-creation
  screen visibly showed translated labels and options after the font fix.
  Visual evidence stays outside Git at `L:\Games\UlutimaUW\LocalizationPrivate\ja-fontfix-char-screen.png`.
  Text contrast against the original book background still needs improvement.
- Full UI layout, longer text wrapping, remaining classic HUD paths and gameplay
  are not yet fully tested. The preview is not a complete translation.

The incremental translation counts (with empty source rows tracked separately) are
recorded in `Docs/TRANSLATION_PROGRESS.md`. The original source has 7,091
nonempty entries; 5,786 are translated, 121 intentionally retain the original
fictional language / parsing terms, and 1,184 still require translation.
All entries in 80 dialogue blocks (3585-3612, 3648-3651, 3672-3674,
3696-3699, 3720-3728, 3730-3733, 3745-3746, 3768-3778 and 3792-3806)
are now reviewed, with zero outstanding entries. Entries in 3728 use
original command/keyword text; the mine code in 3745 and the
Folanae password entry in 3771, and the vault password in 3796 are
likewise preserved.

The intentional exceptions are enumerated in `Localization/ja-preserve.tsv`,
and the coverage script checks them against original nonempty block/index IDs.
These are **not** machine-translated or disguised as Japanese: lizard-speech words,
spoken passwords, mine codes, proper names and conversation lookup keywords must remain
stable to preserve the game's language puzzle and script behaviour.

## Gameplay HUD follow-up

- Character creation is already showing Japanese correctly; do not change that screen
  when working on later HUD text adjustments.
- The classic in-game message log uses the original dark-brown palette colour. For
  ordinary Japanese lines, `UWHudMessageLog` uses light cream text for contrast;
  Japanese text with green/red game colour codes uses brighter pastel green/red.
  English messages retain their existing palette colours.
- The options save list labels unused slots `（未使用）` for display only. The original
  ASCII placeholder, editable save names and on-disk `DESC` data remain untouched.
- The left-side options buttons contain English words *painted into the original
  OPTBTNS.GR sprites*. These are not missing `STRINGS.PAK` translations and require
  separate image/overlay localization. They remain in English in this preview.
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
