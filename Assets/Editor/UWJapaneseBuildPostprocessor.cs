using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// Install the fork's optional translation next to built players. Game data is never copied.
/// </summary>
public static class UWJapaneseBuildPostprocessor
{
    [PostProcessBuild(100)]
    public static void CopyJapaneseTranslations(BuildTarget target, string builtPlayerPath)
    {
        if (target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneLinux64)
            return;

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string source = Path.Combine(projectRoot, "Localization", "ja.tsv");
        if (!File.Exists(source))
            return;

        string playerFolder = Path.GetDirectoryName(builtPlayerPath);
        string destinationFolder = Path.Combine(playerFolder, "Localization");
        Directory.CreateDirectory(destinationFolder);
        File.Copy(source, Path.Combine(destinationFolder, "ja.tsv"), true);
        Debug.Log("Japanese translation copied to: " + destinationFolder);
    }
}
