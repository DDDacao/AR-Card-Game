using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 通关奖励三选一
/// </summary>
public class RewardSelectUI : MonoBehaviour
{
    public GameObject root;
    public TextMeshProUGUI titleText;
    public Transform buttonContainer;
    public GameObject choiceButtonPrefab; // 可选；为空则运行时生成

    public List<Button> choiceButtons = new List<Button>();
    public List<TextMeshProUGUI> choiceLabels = new List<TextMeshProUGUI>();

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

        // 确保脚本所在物体与 root 都激活
        gameObject.SetActive(true);
        if (root == null) root = gameObject;
        root.SetActive(true);
        transform.SetAsLastSibling();
        if (root.transform != transform)
            root.transform.SetAsLastSibling();

        if (titleText != null) titleText.text = "选择一张奖励符咒";

        int count = rewards != null ? rewards.Count : 0;
        EnsureButtons(count);
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            bool has = rewards != null && i < rewards.Count && rewards[i] != null;
            if (choiceButtons[i] == null) continue;
            choiceButtons[i].gameObject.SetActive(has);
            if (!has) continue;

            var card = rewards[i];
            if (choiceLabels[i] != null)
            {
                string label = card.cardName + "\n费" + card.cost + " · " + GetTypeName(card) + "\n" + card.description;
                choiceLabels[i].text = label;
                TmpChineseFontUtil.Apply(choiceLabels[i], label);
            }

            int idx = i;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => Pick(idx));
        }

        Debug.Log($"[RewardSelectUI] Show choices={count}, rootActive={root.activeSelf}");
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void Pick(int index)
    {
        CardDataSO card = null;
        if (options != null && index >= 0 && index < options.Count)
            card = options[index];
        Hide();
        onPicked?.Invoke(card);
        onPicked = null;
    }

    private void EnsureButtons(int count)
    {
        while (choiceButtons.Count < count)
            CreateChoiceButton(choiceButtons.Count);
    }

    private void CreateChoiceButton(int index)
    {
        Transform parent = buttonContainer != null ? buttonContainer : (root != null ? root.transform : transform);
        var go = new GameObject("RewardChoice_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 160);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.12f, 0.1f, 0.95f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.layer = 5;
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8, 8);
        trt.offsetMax = new Vector2(-8, -8);
        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.92f, 0.88f);
        tmp.enableWordWrapping = true;
        // 绑定中文字体并补齐动态图集缺字（避免奖励三选一乱码）
        var cn = TmpChineseFontUtil.FindChineseFont();
        if (cn != null) tmp.font = cn;

        choiceButtons.Add(btn);
        choiceLabels.Add(tmp);
    }

    private static string GetTypeName(CardDataSO c)
    {
        if (c == null) return "";
        switch (c.cardType)
        {
            case CardType.Attack: return "攻击";
            case CardType.Defense: return "防御";
            case CardType.Ability: return "技能";
            case CardType.ArmorBreak: return "破甲";
            case CardType.Seal: return "镇魂";
            case CardType.Fire: return "火符";
            default: return c.cardType.ToString();
        }
    }
}
