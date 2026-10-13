using System;
using UWDataImport.UWData;

namespace UnderworldRevisited.Tools
{
    public static partial class Program
    {
        /// <summary>Regression coverage for sentence fragments composed in code rather than
        /// provided by STRINGS.PAK. They must not mix English grammar with Japanese nouns.</summary>
        private static void fCheckJapaneseComposedMessages()
        {
            bool passed =
                UWObjectDescriptionFormatter.FormatReadMessage("巻物", true) == "巻物にはこう書かれていた……" &&
                UWObjectDescriptionFormatter.FormatReadMessage("scroll", true) == "scrollにはこう書かれていた……" &&
                UWObjectDescriptionFormatter.FormatReadMessage("scroll", false) == "You read the scroll..." &&
                UWObjectDescriptionFormatter.FormatReadMessage(null, false) == "You read the UNNAMED..." &&
                UWObjectDescriptionFormatter.FormatReadMessage(null, true) == "名前のない品にはこう書かれていた……" &&
                UWObjectDescriptionFormatter.FormatLookMessage("a_巻物") == "巻物が見える。";

            if (passed)
                fPass("JP texts", "Japanese read/look messages, Latin names, English fallback");
            else
                fFail("JP texts", "Japanese or English composed message was incorrect");
        }
    }
}
