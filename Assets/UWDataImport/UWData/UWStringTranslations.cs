using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace UWDataImport.UWData
{
    /// <summary>
    /// Optional UTF-8 string overrides. Never writes to the original STRINGS.PAK.
    /// Set UW_JA_TRANSLATIONS to a TSV file containing: block TAB index TAB Japanese text.
    /// Indexes refer to the decoded strings, including the empty slot at index zero.
    /// </summary>
    public static class UWStringTranslations
    {
        public static int Apply(UWStrings strings, string filePath)
        {
            if (strings == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return 0;

            int applied = 0;
            foreach (string rawLine in File.ReadAllLines(filePath, Encoding.UTF8))
            {
                string line = rawLine.TrimStart('\uFEFF');
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                string[] fields = line.Split(new[] { '\t' }, 3);
                if (fields.Length != 3 ||
                    !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int block) ||
                    !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) ||
                    !strings.Blocks.TryGetValue(block, out UWStringBlock stringBlock) ||
                    index <= 0 || index >= stringBlock.Strings.Count)
                    continue;

                stringBlock.Strings[index] = fields[2].Replace("\\n", "\n");
                applied++;
            }
            return applied;
        }
    }
}
