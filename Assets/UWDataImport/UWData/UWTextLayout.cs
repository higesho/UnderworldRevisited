using System.Collections.Generic;
using System.Text;

namespace UWDataImport.UWData
{
	/// <summary>
	/// Text layout in the original fonts without any rendering: character and line spacing,
	/// cleaning up original strings and wrapping them at word boundaries. Engine-free since
	/// 2026-09-17 (P1 of the engine separation); UWFontRenderer on the Unity side renders the
	/// wrapped lines into textures.
	/// </summary>
	public static class UWTextLayout
	{
		/// <summary>
		/// Spacing between two characters in pixels.
		///
		/// Zero, not one. The characters carry no gap of their own, and the font is
		/// italic - the slant already separates them. An extra pixel makes the text
		/// wider than in the original (per user, 2026-09-03), so clearly that he read the
		/// 9674 of his save game as 9874: with the original spacing the 7 places
		/// its left column exactly into the gap that distinguishes a 6 from an 8.
		/// </summary>
		public const int CharacterSpacing = 0;

		/// <summary>
		/// Additional spacing between two lines.
		///
		/// Zero, for the same reason as the character spacing: the characters already bring
		/// their spacing with them. font5x6p is six pixels high, descenders as in g and y sit in the
		/// last of them.
		///
		/// The check: the message box is 30 pixels high and Interaction holds five
		/// messages - five times six gives exactly those 30 (2026-09-03).
		/// </summary>
		public const int LineSpacing = 0;

		/// <summary>
		/// Cleans up an original string for display.
		///
		/// The texts use typewriter quotation marks (`` and '') and contain
		/// two special characters whose exact meaning is not documented: '_' appears
		/// where the sentence has a space ("attacked_._._."), and '^' at the start of
		/// embedded quotations. Here they are treated as a space and as nothing respectively - an
		/// observation, not an evidenced rule.
		/// </summary>
		public static string CleanText(string psText)
		{
			if (string.IsNullOrEmpty(psText))
				return string.Empty;

			return psText
				.Replace("``", "\"")
				.Replace("''", "\"")
				.Replace("^", string.Empty)
				.Replace("_", " ")
				.TrimEnd('\r', '\n');
		}

		/// <summary>Wraps text at word boundaries to a maximum width.</summary>
        public static List<string> WrapText(UWFont pOFont, string psText, int piMaxWidth)
        {
            List<string> lOLines = new List<string>();

            if (pOFont == null || !pOFont.IsLoaded || string.IsNullOrEmpty(psText))
                return lOLines;

            // The original DOS bitmap font cannot measure Japanese characters;
            // each missing glyph otherwise has zero width and long translations
            // never wrap. Its 6px line height is a conservative CJK-cell estimate
            // for the Unity font that will actually draw the translated text.
            if (UWObjectDescriptionFormatter.ContainsJapanese(psText))
                return fWrapJapaneseText(pOFont, psText, piMaxWidth);

			string[] lsWords = psText.Split(' ');
			string lsCurrent = string.Empty;

			// Leading spaces belong to the line. Until 2026-09-03 they were dropped, because the
			// first word of a still empty line was taken over without a separator - with a
			// leading space, however, that first word is empty, and so the space vanished. Four
			// pixels of difference were enough to wrap a line one word too late (measured by the
			// user on the original: the Lakshi letter wraps after "your", ours only
			// after "performing").
			bool lbFirstWord = true;

			foreach (string lsWord in lsWords)
			{
				string lsCandidate = lbFirstWord ? lsWord : lsCurrent + " " + lsWord;

				lbFirstWord = false;

				if (pOFont.MeasureText(lsCandidate, 0, CharacterSpacing) > piMaxWidth && lsCurrent.Length > 0)
				{
					lOLines.Add(lsCurrent);
					lsCurrent = lsWord;
				}
				else
				{
					lsCurrent = lsCandidate;
				}
			}

			if (lsCurrent.Length > 0)
				lOLines.Add(lsCurrent);

            return lOLines;
        }

        private static List<string> fWrapJapaneseText(UWFont font, string text, int maxWidth)
        {
            var result = new List<string>();
            var line = new StringBuilder();
            int width = 0;
            maxWidth = System.Math.Max(1, maxWidth);

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (character == '\r')
                    continue;
                if (character == '\n')
                {
                    result.Add(line.ToString());
                    line.Clear();
                    width = 0;
                    continue;
                }
                if (UWFont.IsColourCode(text, i))
                {
                    line.Append(character).Append(text[++i]);
                    continue;
                }

                // Latin glyphs retain original widths; full-width glyphs take
                // approximately one bitmap-font line height.
                bool fullWidth = character >= 0x3000;
                int advance = fullWidth ? font.Height : font.MeasureText(character.ToString(), 0, CharacterSpacing);
                if (advance <= 0)
                    advance = fullWidth ? font.Height : System.Math.Max(1, font.Height / 2);

                if (width > 0 && width + advance > maxWidth)
                {
                    result.Add(line.ToString());
                    line.Clear();
                    width = 0;
                }
                line.Append(character);
                width += advance;
            }

            if (line.Length > 0)
                result.Add(line.ToString());
            return result;
        }
	}
}
