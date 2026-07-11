using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// 修复中文乱码/方框：把项目里用到的汉字烘焙进 TMP 动态字体图集，并开启 Multi Atlas。
/// 菜单：AR封妖 / 重建中文字体图集（修复乱码）
/// </summary>
public static class RebuildChineseFonts
{
    static readonly string[] FontAssetPaths =
    {
        "Assets/Scripts/Utilities/2.asset",      // 卡牌类型等
        "Assets/Scripts/Utilities/3.asset",      // 卡牌描述 / 费用
        "Assets/Scripts/Utilities/ziti.asset",   // HUD / 奖励 UI
    };

    [MenuItem("AR封妖/重建中文字体图集（修复乱码）")]
    public static void Rebuild()
    {
        string charset = CollectCharset();
        if (string.IsNullOrEmpty(charset))
        {
            Debug.LogError("[RebuildChineseFonts] 未收集到字符。");
            return;
        }

        var report = new StringBuilder();
        report.AppendLine($"收集到 {charset.Length} 个字符。");
        report.AppendLine();

        int okCount = 0;
        foreach (string path in FontAssetPaths)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                report.AppendLine($"[跳过] 找不到: {path}");
                continue;
            }

            // 动态 + 多图集，避免 1024 图集塞满后新字变方框
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.isMultiAtlasTexturesEnabled = true;

            // 通过序列化字段关闭 Clear Dynamic Data on Build（若存在）
            var so = new SerializedObject(font);
            var clearProp = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearProp != null)
            {
                clearProp.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 清旧动态数据再整批加入，避免半残图集
            font.ClearFontAssetData(true);

            string missing = null;
            bool ok = font.TryAddCharacters(charset, out missing);
            EditorUtility.SetDirty(font);

            report.AppendLine($"• {font.name} ({path})");
            report.AppendLine($"  TryAddCharacters: {(ok ? "全部成功" : "部分缺失")}");
            if (!string.IsNullOrEmpty(missing))
            {
                // 源 TTF 本身没有这些字形（毛笔字库常见）
                string show = missing.Length > 80 ? missing.Substring(0, 80) + "…" : missing;
                report.AppendLine($"  源字体缺字({missing.Length}): {show}");
            }
            else
            {
                okCount++;
            }
            report.AppendLine();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 强制刷新使用中的 TMP
        TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, null);

        Debug.Log("[RebuildChineseFonts]\n" + report);
        // 不使用 DisplayDialog，避免 MCP/批处理被模态框卡住
    }

    /// <summary>无弹窗版本，供自动化调用。</summary>
    public static string RebuildSilent()
    {
        Rebuild();
        return "ok";
    }

    /// <summary>从卡牌数据、脚本、策划文案收集需要显示的字符。</summary>
    public static string CollectCharset()
    {
        var set = new HashSet<char>();

        void Add(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            foreach (char c in s)
            {
                if (c == '\0' || char.IsControl(c)) continue;
                set.Add(c);
            }
        }

        // 基础 ASCII / 数字 / 标点（卡牌费用、QTE 计时等）
        Add("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        Add(" ··…—–-_/\\()（）[]【】{}<>《》\"'“”‘’.,，。!！?？:：;；%％+±=×÷@#&*~`|");
        Add("QTEHP");

        // Game Data
        string dataRoot = Path.Combine(Application.dataPath, "Game Data");
        if (Directory.Exists(dataRoot))
        {
            foreach (string file in Directory.GetFiles(dataRoot, "*.asset", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file, Encoding.UTF8);
                // YAML unicode escapes
                foreach (Match m in Regex.Matches(text, @"\\u([0-9a-fA-F]{4})"))
                    set.Add((char)int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber));
                // raw CJK if any
                foreach (char c in text)
                    if (c >= 0x4E00 && c <= 0x9FFF) set.Add(c);
            }
        }

        // Scripts 中文字符串
        string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
        if (Directory.Exists(scriptsRoot))
        {
            foreach (string file in Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file, Encoding.UTF8);
                foreach (char c in text)
                    if (c >= 0x4E00 && c <= 0x9FFF) set.Add(c);
            }
        }

        // 编辑器菜单里写死的文案
        string editorRoot = Path.Combine(Application.dataPath, "Editor");
        if (Directory.Exists(editorRoot))
        {
            foreach (string file in Directory.GetFiles(editorRoot, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file, Encoding.UTF8);
                foreach (char c in text)
                    if (c >= 0x4E00 && c <= 0x9FFF) set.Add(c);
            }
        }

        // 卡牌类型中文标签等硬编码补充
        Add("攻击防御技能破甲镇魂火符");
        Add("命中破绽连敲镇魂铃快速点按铃铛次镇魂成功铃音未成");
        Add("普通攻击正在防御蓄力中");
        Add("小妖石灵山鬼");
        Add("造成点伤害附加层灼烧引爆破甲打断护甲灵气回复获得");
        Add("红破绽黄裂纹紫封印可QTE");
        Add("三关尽破封妖成功封印失败重试本关再来一局");
        Add("选择一张奖励符咒带入后续符匣");

        var sb = new StringBuilder(set.Count);
        foreach (char c in set)
            sb.Append(c);
        return sb.ToString();
    }
}
