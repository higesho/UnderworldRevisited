# Japanese localization workstream

Branch: `japanese-localization` (based on the upstream `main`).

## Current implementation

The first translated in-game messages are in `Localization/ja.tsv`.
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
- The sample translations were loaded successfully from the GOG installation
  by the engine-free `Tools/UWDump` tool, with and without the environment variable.
- Japanese glyph rendering and in-game layout have **not** yet been visually verified.
- The preview build is not a complete translation (currently seven strings).

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

`STRINGS.PAK` is decoded as before. The optional `UW_JA_TRANSLATIONS`
environment variable points to a UTF-8 TSV file with three columns:

```
# block<TAB>index<TAB>translation
1	1	翻訳例
```

The example is a format demonstration, **not** a verified translation for block 1/index 1.
Only nonzero, existing string indices are replaced; all unspecified strings retain
their original wording. `\\n` in the translation becomes a newline. This leaves the
original game files untouched and makes upstream synchronization easier.

Example on Windows PowerShell, before launching the game in the same shell:

```powershell
$env:UW_JA_TRANSLATIONS = 'C:\Translations\underworld-ja.tsv'
```

## Outstanding work

The modern HUD now tries an installed OS Japanese font (Noto Sans CJK JP,
Noto Sans JP, Yu Gothic, Meiryo or MS Gothic) when the text contains Japanese.
This path must still be verified in Unity. For Linux, install Noto Sans CJK JP;
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
