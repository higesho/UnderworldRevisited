using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Localizes the original OPTBTNS.GR options menu without shipping modified copyrighted
/// picture data. The game's own sprite is decoded as usual, but the few palette indices
/// used to paint Latin letters and their adjacent black drop shadows are replaced by
/// nearby background pixels at runtime. The original button outlines, selection art,
/// Roman save-slot numbers, hit rectangles and palette shader remain unchanged.
/// </summary>
public static class UWJapaneseOptionsArt
{
    private static readonly Dictionary<int, string> Labels = new Dictionary<int, string>
    {
        { 6, "セーブ" }, { 7, "セーブ" }, { 8, "ロード" }, { 9, "ロード" },
        { 10, "音楽" }, { 11, "音楽" }, { 12, "効果音" }, { 13, "効果音" },
        { 14, "画質" }, { 15, "画質" }, { 16, "戻る" }, { 17, "戻る" },
        { 18, "終了" }, { 19, "終了" }, { 20, "入" }, { 21, "入" },
        { 22, "切" }, { 23, "切" }, { 24, "取消" }, { 25, "取消" },
        { 26, "完了" }, { 27, "完了" }, { 28, "完了" }, { 29, "完了" },
        { 38, "低" }, { 39, "低" }, { 40, "中" }, { 41, "中" },
        { 42, "高" }, { 43, "高" }, { 44, "最高" }, { 45, "最高" },
        { 46, "ロード" },
        { 47, "音楽：入" }, { 48, "音楽：切" },
        { 49, "効果音：入" }, { 50, "効果音：切" },
        { 51, "音楽切替" }, { 52, "音声切替" },
        { 53, "現在：低" }, { 54, "現在：中" },
        { 55, "現在：高" }, { 56, "現在：最高" },
        { 57, "はい" }, { 58, "はい" }, { 59, "いいえ" }, { 60, "いいえ" }
    };

    private const string TextObjectName = "Japanese Options Label";

    /// <summary>
    /// A disabled Unity UI Image does not disable its child Text components.
    /// Always hide or show the localized lettering alongside the original image;
    /// otherwise options captions leak over the six classic action icons even
    /// when the options panel is closed.
    /// </summary>
    public static void SetVisible(Image picture, bool visible)
    {
        if (picture == null) return;

        picture.enabled = visible;
        Transform caption = picture.transform.Find(TextObjectName);
        if (caption != null)
            caption.gameObject.SetActive(visible && LabelForPicture(picture));
    }

    private static bool LabelForPicture(Image picture)
    {
        Transform caption = picture.transform.Find(TextObjectName);
        Text label = caption != null ? caption.GetComponent<Text>() : null;
        return label != null && !string.IsNullOrEmpty(label.text);
    }

    public static bool NeedsCleaning(int pictureIndex)
    {
        // 1-5 are the complete pre-painted options panels. Their visible entries are
        // overlaid with individual translated button images; headings 3/5 need captions.
        return (pictureIndex >= 1 && pictureIndex <= 5) || Labels.ContainsKey(pictureIndex);
    }

    public static string LabelFor(int pictureIndex)
    {
        return Labels.TryGetValue(pictureIndex, out string text) ? text : null;
    }

    private static bool IsLetterIndex(byte index)
    {
        // Normal button ink (0x62), highlighted button ink (0x60), the grey
        // YES/NO variant (0x92). Pure black (0x01) is also used for outer
        // borders, so it is only erased when adjacent to one of these inks.
        return index == 98 || index == 96 || index == 146;
    }

    /// <summary>Patch the *palette-index* texture returned by UWIconTextureBuilder.
    /// Both reads and writes operate on a copy; no original STRINGS.PAK or GR is altered.</summary>
    public static void EraseLatinInk(Texture2D texture)
    {
        if (texture == null) return;
        int width = texture.width, height = texture.height;
        Color32[] original = texture.GetPixels32();
        bool[] ink = new bool[original.Length];
        bool[] erase = new bool[original.Length];
        bool found = false;

        for (int i = 0; i < original.Length; i++)
        {
            ink[i] = original[i].a != 0 && IsLetterIndex(original[i].r);
            erase[i] = ink[i];
            if (ink[i]) found = true;
        }
        if (!found) return;

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int at = y * width + x;
            if (original[at].a == 0 || original[at].r != 1) continue;
            for (int dy = -1; dy <= 1 && !erase[at]; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int xx = x + dx, yy = y + dy;
                if (xx >= 0 && xx < width && yy >= 0 && yy < height &&
                    ink[yy * width + xx])
                {
                    erase[at] = true;
                    break;
                }
            }
        }

        Color32[] cleaned = (Color32[])original.Clone();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int at = y * width + x;
            if (!erase[at]) continue;

            // Use the nearest originally unpainted pixel, preferring the same row.
            // This preserves the natural red/brown marbling and the pressed pink
            // colour rather than putting an opaque rectangle over the original art.
            int source = -1;
            for (int radius = 1; radius < width && source < 0; radius++)
            {
                int left = x - radius, right = x + radius;
                if (left >= 0 && !erase[y * width + left] && original[y * width + left].a != 0)
                    source = y * width + left;
                else if (right < width && !erase[y * width + right] && original[y * width + right].a != 0)
                    source = y * width + right;
            }
            for (int radius = 1; radius < height && source < 0; radius++)
            {
                int down = y - radius, up = y + radius;
                if (down >= 0 && !erase[down * width + x] && original[down * width + x].a != 0)
                    source = down * width + x;
                else if (up < height && !erase[up * width + x] && original[up * width + x].a != 0)
                    source = up * width + x;
            }
            if (source >= 0) cleaned[at] = original[source];
        }
        texture.SetPixels32(cleaned);
        texture.Apply(false, false);
    }

    /// <summary>Add or update a decorative label. The mouse/keyboard always uses the
    /// original parent Image rect; child texts do not receive pointer events.</summary>
    public static void ApplyLabel(Image picture, int pictureIndex, Font font)
    {
        if (picture == null) return;
        Transform existing = picture.transform.Find(TextObjectName);
        string text = LabelFor(pictureIndex);
        if (pictureIndex == 3) text = "終了\nしますか？";
        if (pictureIndex == 5) text = "画質を選択";

        if (font == null || text == null)
        {
            if (existing != null)
            {
                Text previous = existing.GetComponent<Text>();
                if (previous != null) previous.text = string.Empty;
                existing.gameObject.SetActive(false);
            }
            return;
        }

        Text label;
        if (existing == null)
        {
            GameObject child = new GameObject(TextObjectName, typeof(RectTransform), typeof(Text), typeof(Shadow));
            child.transform.SetParent(picture.transform, false);
            label = child.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontStyle = FontStyle.Bold;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = false;
            label.raycastTarget = false;
            Shadow shadow = child.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(0.5f, -0.5f);
        }
        else
        {
            existing.gameObject.SetActive(true);
            label = existing.GetComponent<Text>();
        }

        RectTransform rect = (RectTransform)label.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        float width = picture.sprite != null ? picture.sprite.rect.width : 31f;
        float height = picture.sprite != null ? picture.sprite.rect.height : 14f;
        float top = 0f;
        if (pictureIndex == 3) height = 34f;
        if (pictureIndex == 5) { top = 20f; height = 13f; }
        rect.anchoredPosition = new Vector2(1f, -top);
        rect.sizeDelta = new Vector2(width - 2f, height);

        label.font = font;
        label.text = text;
        label.gameObject.SetActive(picture.enabled);
        int longest = 0;
        foreach (string line in text.Split('\n')) longest = Mathf.Max(longest, line.Length);
        // These are 31-pixel-wide original buttons. A bold 6-9px font can fit
        // 2-5 full-width Japanese glyphs without clipping the decorated edges.
        label.fontSize = Mathf.Clamp(Mathf.FloorToInt((width - 1f) / Mathf.Max(1, longest)), 6, 9);
        label.lineSpacing = 0.88f;
        label.color = (pictureIndex == 3 || pictureIndex == 5)
            ? new Color32(246, 226, 188, 255)
            : new Color32(251, 240, 216, 255);
    }
}
