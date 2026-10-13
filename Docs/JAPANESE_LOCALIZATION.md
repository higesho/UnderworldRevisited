# Japanese localization workstream

Branch: `japanese-localization` (based on the upstream `main`).

## Current implementation

The translated in-game messages are in `Localization/ja.tsv` (210 entries:
159 general messages, 49 character creation labels, 2 conversation messages).
Set `UW_JA_TRANSLATIONS` to that file's full path to enable them,
or place `Localization/ja.tsv` next to the executable for automatic discovery.
When running from the Unity Editor the project root's `Localization/ja.tsv` is
loaded automatically as well. A post-build hook copies it into Windows and Linux
players, so no environment variable is needed for a normal fork build.
Translation coverage is still only a small sample, not a full localization.

## Build verification (2026-10-13)

- Unity 6000.6.0f1 imported and compiled all project scripts successfully.
- A Windows x64 player built successfully, and the post-build hook copied
  `Localization/ja.tsv` beside `UR.exe`.
- The translations were loaded successfully from the GOG installation
  by the engine-free `Tools/UWDump` tool, with and without the environment variable.
- `uwdump validate-tsv` checked 210 rows against the original strings: zero
  invalid keys, duplicate keys, newline differences, or control-code warnings.
- The Editor smoke test (`UWJapaneseFontDiagnostics.VerifyJapaneseFont`) selected
  Noto Sans JP, found every tested Japanese glyph, generated 68 uGUI vertices,
  and verified Japanese/Latin per-label font switching. The test exited normally.
- `UWJapaneseFontDiagnostics.VerifyJapaneseClassicLabels` verified that the
  character-creation UI displays CJK with Noto Sans JP while untranslated
  Latin text still uses the original bitmap font in classic mode.
- The original `STRINGS.PAK` remains the source for 10,896 untranslated entries,
  checked byte-for-byte against the decoded originals by `validate-tsv`.
- The Windows player was launched with the GOG data and its character-creation
  screen visibly showed translated labels and options after the font fix.
  Visual evidence stays outside Git at `L:\Games\UlutimaUW\LocalizationPrivate\ja-fontfix-char-screen.png`.
  Text contrast against the original book background still needs improvement.
- Full UI layout, longer text wrapping, remaining classic HUD paths and gameplay
  are not yet fully tested. The preview is not a complete translation.

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
