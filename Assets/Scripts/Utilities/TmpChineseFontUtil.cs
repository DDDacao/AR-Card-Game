using UnityEngine;
using TMPro;

/// <summary>
/// 运行时确保 TMP 动态字体能显示文本中的汉字（避免方框/乱码）。
/// </summary>
public static class TmpChineseFontUtil
{
    static TMP_FontAsset cachedUiFont;

    /// <summary>
    /// 新版 HUD / 意图 / 回合等会反复用到的字符，Awake 时预热进动态图集。
    /// 只放常用字与安全标点，避免毛笔字库本身缺的生僻字。
    /// </summary>
    public const string HudWarmupCharset =
        "0123456789/:" +
        "第回合玩家妖怪行动中等待开战战斗结束可重试或返回扫描封妖阵开始" +
        "攻击防御蓄力普通正在准备重击造成点伤害获得护甲已被打断" +
        "弱点红色黄色紫色用斩妖符破煞符镇魂符瞄准对准拖出拖牌" +
        "本无暴露牌堆剩余手牌结束请稍候点击击破继续出牌" +
        "小妖石灵山鬼战斗胜利封印成功三关尽破封妖请扫描关卡牌挑战再来一局重试确定" +
        "：， ";

    /// <summary>项目内优先用的中文字体（2 / 3 / ziti）。</summary>
    public static TMP_FontAsset FindChineseFont()
    {
        if (cachedUiFont != null) return cachedUiFont;

        // 优先 2/3（端宁毛笔，图集较全）；ziti 衡山毛笔缺字较多作备选
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

    /// <summary>绑定中文字体 + 正确材质，并把字符串缺字补进动态图集。</summary>
    public static void Apply(TMP_Text tmp, string text = null)
    {
        if (tmp == null) return;

        BindChineseFont(tmp);

        string s = text ?? tmp.text;
        EnsureCharacters(tmp.font, s);
    }

    /// <summary>强制使用项目中文字体，并同步 fontSharedMaterial（否则会乱码）。</summary>
    public static void BindChineseFont(TMP_Text tmp)
    {
        if (tmp == null) return;
        var font = FindChineseFont();
        if (font == null) return;

        if (tmp.font != font)
            tmp.font = font;

        // 换字体后若仍挂着 LiberationSans 的材质，会整段乱码/方框
        if (font.material != null && tmp.fontSharedMaterial != font.material)
            tmp.fontSharedMaterial = font.material;
    }

    /// <summary>预热 HUD 常用字，减少首次刷新缺字。</summary>
    public static void WarmupHudCharset()
    {
        EnsureCharacters(FindChineseFont(), HudWarmupCharset);
    }

    public static void EnsureCharacters(TMP_FontAsset font, string text)
    {
        if (font == null || string.IsNullOrEmpty(text)) return;

        // 静态图集也可能缺字；尽量切动态
        if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        if (!font.isMultiAtlasTexturesEnabled)
            font.isMultiAtlasTexturesEnabled = true;

        font.TryAddCharacters(text);
    }
}
