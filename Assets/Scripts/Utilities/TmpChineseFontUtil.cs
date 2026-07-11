using UnityEngine;
using TMPro;

/// <summary>
/// 运行时确保 TMP 动态字体能显示文本中的汉字（避免方框/乱码）。
/// </summary>
public static class TmpChineseFontUtil
{
    static TMP_FontAsset cachedUiFont;

    /// <summary>项目内优先用的中文字体（ziti / 2 / 3）。</summary>
    public static TMP_FontAsset FindChineseFont()
    {
        if (cachedUiFont != null) return cachedUiFont;

        // 优先 2/3（端宁毛笔，图集已含全量玩法汉字）；ziti 衡山毛笔缺字较多作备选
        string[] prefer = { "2", "3", "ziti" };
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int p = 0; p < prefer.Length; p++)
        {
            for (int i = 0; i < fonts.Length; i++)
            {
                if (fonts[i] != null && fonts[i].name == prefer[p])
                {
                    cachedUiFont = fonts[i];
                    return cachedUiFont;
                }
            }
        }
        return null;
    }

    /// <summary>绑定字体并把当前字符串缺字补进动态图集。</summary>
    public static void Apply(TMP_Text tmp, string text = null)
    {
        if (tmp == null) return;

        var font = tmp.font != null ? tmp.font : FindChineseFont();
        if (font != null && tmp.font != font)
            tmp.font = font;

        string s = text ?? tmp.text;
        EnsureCharacters(tmp.font, s);
    }

    public static void EnsureCharacters(TMP_FontAsset font, string text)
    {
        if (font == null || string.IsNullOrEmpty(text)) return;
        if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic) return;

        // 尽量打开多图集，避免 1024 图集满后新字失败
        if (!font.isMultiAtlasTexturesEnabled)
            font.isMultiAtlasTexturesEnabled = true;

        font.TryAddCharacters(text);
    }
}
