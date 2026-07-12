using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 奖励三选一：每个选项都是一张完整的可点击卡牌，而非文字面板。
/// </summary>
public class RewardSelectUI : MonoBehaviour
{
    public GameObject root;
    public TextMeshProUGUI titleText;
    public Transform buttonContainer;

    [Header("完整卡牌底图")]
    public Sprite cardBaseSprite;

    // 兼容现有的 ApplyPlanCardData 编辑器工具；正式奖励展示由下方运行时列表管理。
    public List<Button> choiceButtons = new List<Button>();
    public List<TextMeshProUGUI> choiceLabels = new List<TextMeshProUGUI>();
    public List<Image> choiceCardImages = new List<Image>();
    public List<TextMeshProUGUI> choiceNameLabels = new List<TextMeshProUGUI>();

    private readonly List<RawImage> choiceArts = new List<RawImage>();
    private readonly List<TextMeshProUGUI> choiceNames = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> choiceDescriptions = new List<TextMeshProUGUI>();

    private Action<CardDataSO> onPicked;
    private List<CardDataSO> options;

    private void Awake()
    {
        if (root == null) root = gameObject;
        Hide();
    }

    public void Show(List<CardDataSO> rewards, Action<CardDataSO> onPick)
    {
        options = rewards;
        onPicked = onPick;
        gameObject.SetActive(true);
        if (root == null) root = gameObject;
        root.SetActive(true);
        transform.SetAsLastSibling();
        if (root.transform != transform) root.transform.SetAsLastSibling();

        if (titleText != null)
            SetChineseText(titleText, "选择一张奖励符咒");

        // 每次奖励都重建三张真实卡牌，避免旧版文字按钮或图标面板残留。
        ClearChoices();
        int count = rewards != null ? Mathf.Min(3, rewards.Count) : 0;
        for (int i = 0; i < count; i++)
        {
            if (rewards[i] == null) continue;
            CreateChoiceCard(i, rewards[i]);
        }
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void CreateChoiceCard(int index, CardDataSO card)
    {
        Transform parent = buttonContainer != null ? buttonContainer : (root != null ? root.transform : transform);
        var go = new GameObject("RewardChoiceCard_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(270, 430);

        var baseImage = go.GetComponent<Image>();
        baseImage.sprite = cardBaseSprite;
        baseImage.color = cardBaseSprite != null ? Color.white : new Color(.16f, .13f, .1f, 1f);
        baseImage.preserveAspect = true;
        var button = go.GetComponent<Button>();
        button.targetGraphic = baseImage;
        button.onClick.AddListener(() => Pick(index));

        // 用完整卡牌底图承载奖励图案；奖励素材本身带有大面积透明留白，故用 UV 裁切显示中央符咒图案。
        var artGo = new GameObject("CardArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        artGo.layer = 5;
        artGo.transform.SetParent(go.transform, false);
        var artRt = artGo.GetComponent<RectTransform>();
        artRt.anchorMin = artRt.anchorMax = new Vector2(.5f, .62f);
        artRt.pivot = new Vector2(.5f, .5f);
        artRt.sizeDelta = new Vector2(188, 178);
        var art = artGo.GetComponent<RawImage>();
        art.raycastTarget = false;
        if (card.cardImage != null)
        {
            art.texture = card.cardImage.texture;
            art.uvRect = new Rect(.10f, .18f, .80f, .62f);
        }
        else art.enabled = false;

        var name = CreateText("NameAndCost", go.transform, new Vector2(.5f, .30f), new Vector2(230, 34), 22, TextAlignmentOptions.Center, new Color(.35f, .16f, .08f));
        SetChineseText(name, $"{card.cardName} · 费用 {card.cost}");

        var desc = CreateText("Description", go.transform, new Vector2(.5f, .16f), new Vector2(226, 78), 16, TextAlignmentOptions.Center, new Color(.22f, .16f, .11f));
        SetChineseText(desc, $"{GetTypeName(card)}\n{card.description}");

        choiceButtons.Add(button);
        choiceArts.Add(art);
        choiceNames.Add(name);
        choiceDescriptions.Add(desc);
    }

    private void Pick(int index)
    {
        CardDataSO card = options != null && index >= 0 && index < options.Count ? options[index] : null;
        Hide();
        onPicked?.Invoke(card);
        onPicked = null;
    }

    private void ClearChoices()
    {
        choiceButtons.Clear();
        choiceArts.Clear();
        choiceNames.Clear();
        choiceDescriptions.Clear();

        Transform parent = buttonContainer != null ? buttonContainer : (root != null ? root.transform : transform);
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith("RewardChoice")) Destroy(child.gameObject);
        }
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchor, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static void SetChineseText(TextMeshProUGUI text, string value)
    {
        text.text = value;
        TmpChineseFontUtil.Apply(text, value);
    }

    private static string GetTypeName(CardDataSO card)
    {
        switch (card.cardType)
        {
            case CardType.Attack: return "攻击符";
            case CardType.Defense: return "防御符";
            case CardType.Ability: return "技能符";
            case CardType.ArmorBreak: return "破甲符";
            case CardType.Seal: return "镇魂符";
            case CardType.Fire: return "火符";
            default: return "符咒";
        }
    }
}
