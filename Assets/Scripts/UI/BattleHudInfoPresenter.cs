using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 可调 HUD 文案：Boss 名、攻击意图（仅一行名）、回合状态、操作提示。
/// </summary>
public sealed class BattleHudInfoPresenter : MonoBehaviour
{
    [Header("可在场景中摆放的文字")]
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI enemyIntentText;
    public TextMeshProUGUI endTurnText;
    public TextMeshProUGUI turnStateText;
    public TextMeshProUGUI hintText;

    [Header("可选")]
    public TextMeshProUGUI deckCountText;
    public Image intentBadgeBackground;

    [Header("样式")]
    [Tooltip("仅在父节点有非均匀缩放时，把意图/名称挪到 HUD 根下，避免被血条缩放压扁。不会每帧改你手调的 size/位置。")]
    public bool detachTextFromScaledParents = true;
    public Color colorIdle = new Color(0.96f, 0.94f, 0.90f, 1f);
    public Color colorPlayerTurn = new Color(0.98f, 0.90f, 0.55f, 1f);
    public Color colorEnemyTurn = new Color(1f, 0.62f, 0.48f, 1f);
    public Color colorIntent = new Color(1f, 0.92f, 0.75f, 1f);
    public Color colorMuted = new Color(0.88f, 0.86f, 0.80f, 0.96f);

    private TurnManager turnManager;
    private CardDeck cardDeck;
    private CharacterStats enemy;
    private EnemyIntentController intent;
    private float nextRefresh;
    private bool layoutPrepared;
    private string lastIntentKey;
    private string lastTurnKey;
    private string lastHintKey;

    private static readonly Color IntentBadgeBg = new Color(0.08f, 0.07f, 0.14f, 0.75f);

    private void Awake()
    {
        TmpChineseFontUtil.WarmupHudCharset();
        BindAllFonts();
        if (endTurnText != null)
            SetText(endTurnText, "\u7ed3\u675f\u56de\u5408", colorIdle); // 结束回合
        PrepareLayoutOnce();
    }

    private void OnEnable()
    {
        BindAllFonts();
        BindTurnEvents();
        RefreshTexts(true);
    }

    private void OnDisable()
    {
        UnbindTurnEvents();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.1f;
        ResolveReferences();
        RefreshTexts(false);
    }

    private void BindAllFonts()
    {
        TmpChineseFontUtil.BindChineseFont(bossNameText);
        TmpChineseFontUtil.BindChineseFont(enemyIntentText);
        TmpChineseFontUtil.BindChineseFont(endTurnText);
        TmpChineseFontUtil.BindChineseFont(turnStateText);
        TmpChineseFontUtil.BindChineseFont(hintText);
        TmpChineseFontUtil.BindChineseFont(deckCountText);
    }

    private void PrepareLayoutOnce()
    {
        if (layoutPrepared) return;
        layoutPrepared = true;

        // Boss 血条美术节点常有非均匀 scale（例如 Y=0.19）。
        // 意图/名称若挂在其下会被压扁变糊；挪到 HUD 根下，并保持世界位置。
        // 之后不再改 sizeDelta / anchoredPosition，方便你在编辑器手调。
        if (detachTextFromScaledParents)
        {
            DetachFromNonUniformScaleParent(bossNameText);
            DetachFromNonUniformScaleParent(enemyIntentText);
        }

        if (enemyIntentText != null)
        {
            enemyIntentText.richText = false;
            enemyIntentText.enableWordWrapping = false;
            enemyIntentText.overflowMode = TextOverflowModes.Overflow;
            enemyIntentText.alignment = TextAlignmentOptions.Center;
            // 不强制 fontSize / size，尊重场景手调；仅在过小且仍被压扁时兜底
            if (enemyIntentText.fontSize < 18f)
                enemyIntentText.fontSize = 28f;

            var rt = enemyIntentText.rectTransform;
            NormalizeUniformScale(rt);
            EnsureIntentBadgeBackground();
            // 背景跟着文字走，首次按当前文案收紧
            FitIntentBoxToText();
        }

        if (bossNameText != null)
        {
            bossNameText.richText = false;
            NormalizeUniformScale(bossNameText.rectTransform);
        }

        if (turnStateText != null)
        {
            turnStateText.richText = false;
            turnStateText.enableWordWrapping = true;
        }

        if (hintText != null)
        {
            hintText.richText = false;
            hintText.enableWordWrapping = true;
        }
    }

    /// <summary>
    /// 若祖先有非均匀缩放，把节点挂到 HUD_ArtSkin_Adjustable（或 Canvas）下，避免被血条 scale 压扁。
    /// </summary>
    private static void DetachFromNonUniformScaleParent(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        var rt = tmp.rectTransform;
        if (!HasNonUniformScaleInParents(rt)) return;

        Transform hudRoot = FindHudRoot(rt);
        if (hudRoot == null || rt.parent == hudRoot) return;

        // 保持屏幕位置
        Vector3 worldPos = rt.position;
        Quaternion worldRot = rt.rotation;
        rt.SetParent(hudRoot, true);
        rt.position = worldPos;
        rt.rotation = worldRot;
        rt.localScale = Vector3.one;
    }

    private static bool HasNonUniformScaleInParents(Transform t)
    {
        Transform p = t != null ? t.parent : null;
        while (p != null)
        {
            Vector3 s = p.localScale;
            if (Mathf.Abs(s.x - s.y) > 0.02f
                || Mathf.Abs(s.x - s.z) > 0.02f
                || Mathf.Abs(s.y - s.z) > 0.02f
                || s.x < 0.5f || s.y < 0.5f)
                return true;
            // 到 Canvas 为止
            if (p.GetComponent<Canvas>() != null) break;
            p = p.parent;
        }
        return false;
    }

    private static Transform FindHudRoot(Transform from)
    {
        Transform t = from;
        while (t != null)
        {
            if (t.name == "HUD_ArtSkin_Adjustable") return t;
            t = t.parent;
        }
        // 回退：Canvas
        t = from;
        while (t != null)
        {
            if (t.GetComponent<Canvas>() != null) return t;
            t = t.parent;
        }
        return from != null ? from.root : null;
    }

    private static void NormalizeUniformScale(RectTransform rt)
    {
        if (rt == null) return;
        Vector3 s = rt.localScale;
        if (Mathf.Abs(s.x - 1f) > 0.01f || Mathf.Abs(s.y - 1f) > 0.01f || Mathf.Abs(s.z - 1f) > 0.01f)
            rt.localScale = Vector3.one;
    }

    private void EnsureIntentBadgeBackground()
    {
        if (enemyIntentText == null) return;

        if (intentBadgeBackground == null)
        {
            Transform existing = enemyIntentText.transform.Find("IntentBadgeBg");
            if (existing != null)
                intentBadgeBackground = existing.GetComponent<Image>();
        }

        if (intentBadgeBackground == null)
        {
            var go = new GameObject("IntentBadgeBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = enemyIntentText.gameObject.layer;
            go.transform.SetParent(enemyIntentText.transform, false);
            go.transform.SetAsFirstSibling();

            var img = go.GetComponent<Image>();
            img.color = IntentBadgeBg;
            img.raycastTarget = false;
            intentBadgeBackground = img;
        }

        // 背景贴满文字 Rect（文字框本身会按 preferred 收紧），只留很小边距
        var bgRt = intentBadgeBackground.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(-14f, -6f);
        bgRt.offsetMax = new Vector2(14f, 6f);
        bgRt.localScale = Vector3.one;
    }

    /// <summary>
    /// 把意图文字框收成「刚好包住文字」的大小，背景随之变窄。
    /// </summary>
    private void FitIntentBoxToText()
    {
        if (enemyIntentText == null) return;

        enemyIntentText.ForceMeshUpdate(true);
        float padX = 28f; // 左右内边距
        float padY = 14f; // 上下内边距
        float w = enemyIntentText.preferredWidth + padX;
        float h = enemyIntentText.preferredHeight + padY;

        // 空文本时给个最小占位，避免闪成 0
        if (w < 80f) w = 80f;
        if (h < 36f) h = 36f;
        w = Mathf.Min(w, 700f);
        h = Mathf.Min(h, 90f);

        var rt = enemyIntentText.rectTransform;
        // 保持中心点，只改宽高
        rt.sizeDelta = new Vector2(w, h);
    }

    private void BindTurnEvents()
    {
        ResolveReferences();
        if (turnManager == null) return;
        turnManager.OnTurnInfoChanged -= OnTurnEvent;
        turnManager.OnBattleStarted -= OnTurnEvent;
        turnManager.OnPlayerTurnStarted -= OnTurnEvent;
        turnManager.OnEnemyTurnStarted -= OnTurnEvent;
        turnManager.OnTurnInfoChanged += OnTurnEvent;
        turnManager.OnBattleStarted += OnTurnEvent;
        turnManager.OnPlayerTurnStarted += OnTurnEvent;
        turnManager.OnEnemyTurnStarted += OnTurnEvent;
    }

    private void UnbindTurnEvents()
    {
        if (turnManager == null) return;
        turnManager.OnTurnInfoChanged -= OnTurnEvent;
        turnManager.OnBattleStarted -= OnTurnEvent;
        turnManager.OnPlayerTurnStarted -= OnTurnEvent;
        turnManager.OnEnemyTurnStarted -= OnTurnEvent;
    }

    private void OnTurnEvent()
    {
        ResolveReferences();
        RefreshTexts(true);
    }

    private void ResolveReferences()
    {
        TurnManager found = TurnManager.Instance != null
            ? TurnManager.Instance
            : FindAnyObjectByType<TurnManager>();

        if (found != turnManager)
        {
            UnbindTurnEvents();
            turnManager = found;
            BindTurnEvents();
        }

        if (turnManager == null) return;

        if (turnManager.enemyStats != null) enemy = turnManager.enemyStats;
        if (turnManager.enemyIntent != null) intent = turnManager.enemyIntent;
        if (intent == null && enemy != null)
            intent = enemy.GetComponentInChildren<EnemyIntentController>();
        if (turnManager.cardDeck != null) cardDeck = turnManager.cardDeck;
        if (cardDeck == null) cardDeck = FindAnyObjectByType<CardDeck>();
    }

    private void RefreshTexts(bool force)
    {
        if (bossNameText != null)
        {
            string name = enemy != null && enemy.templateData != null
                          && !string.IsNullOrWhiteSpace(enemy.templateData.characterName)
                ? enemy.templateData.characterName
                : "\u5996\u602a"; // 妖怪
            SetText(bossNameText, name, colorIdle);
        }

        RefreshIntent(force);
        RefreshTurnState(force);
        RefreshHint(force);
    }

    /// <summary>
    /// 攻击意图：只显示一行，例如「普通攻击」。
    /// </summary>
    private void RefreshIntent(bool force)
    {
        if (enemyIntentText == null) return;

        string value;
        if (intent != null && intent.CurrentStep != null
            && !string.IsNullOrWhiteSpace(intent.CurrentStep.displayName))
        {
            value = intent.CurrentStep.displayName.Trim();
        }
        else if (intent != null && !string.IsNullOrWhiteSpace(intent.CurrentDisplayName))
        {
            value = intent.CurrentDisplayName.Trim();
        }
        else if (turnManager != null && !string.IsNullOrWhiteSpace(turnManager.CurrentEnemyIntent))
        {
            value = turnManager.CurrentEnemyIntent.Trim();
        }
        else
        {
            value = "\u2014"; // —
        }

        // 蓄力被打断时仍只改这一行名字，不叠额外说明
        if (intent != null && intent.ChargeInterrupted
            && intent.CurrentStep != null
            && intent.CurrentStep.kind == EnemyIntentKind.Charge)
        {
            value = "\u84c4\u529b\u5df2\u6253\u65ad"; // 蓄力已打断
        }

        if (!force && value == lastIntentKey) return;
        lastIntentKey = value;

        if (intentBadgeBackground != null)
            intentBadgeBackground.color = IntentBadgeBg;

        SetText(enemyIntentText, value, colorIntent);
        FitIntentBoxToText();
    }

    private void RefreshTurnState(bool force)
    {
        if (turnStateText == null && deckCountText == null) return;

        string turnLine;
        string phaseLine;
        Color phaseColor = colorIdle;

        if (turnManager == null || !turnManager.IsBattleActive)
        {
            if (turnManager != null && turnManager.BattleEnded)
            {
                turnLine = "\u6218\u6597\u7ed3\u675f"; // 战斗结束
                phaseLine = "\u53ef\u91cd\u8bd5"; // 可重试
                phaseColor = colorMuted;
            }
            else
            {
                turnLine = "\u7b49\u5f85\u5f00\u6218"; // 等待开战
                phaseLine = "\u626b\u63cf\u5c01\u5996\u9635"; // 扫描封妖阵
                phaseColor = colorMuted;
            }
        }
        else
        {
            turnLine = "\u7b2c" + turnManager.CurrentTurnIndex + "\u56de\u5408"; // 第N回合
            if (turnManager.IsPlayerTurn)
            {
                phaseLine = "\u73a9\u5bb6\u56de\u5408"; // 玩家回合
                phaseColor = colorPlayerTurn;
            }
            else
            {
                phaseLine = "\u5996\u602a\u884c\u52a8\u4e2d"; // 妖怪行动中
                phaseColor = colorEnemyTurn;
            }
        }

        string deckLine = BuildDeckLine();
        string value = string.IsNullOrEmpty(deckLine)
            ? turnLine + "\n" + phaseLine
            : turnLine + "\n" + phaseLine + "\n" + deckLine;

        if (!force && value == lastTurnKey) return;
        lastTurnKey = value;

        if (deckCountText != null)
        {
            SetText(turnStateText, turnLine + "\n" + phaseLine, phaseColor);
            SetText(deckCountText, deckLine, colorMuted);
        }
        else if (turnStateText != null)
        {
            SetText(turnStateText, value, phaseColor);
        }
    }

    private void RefreshHint(bool force)
    {
        if (hintText == null) return;

        string value;
        if (turnManager == null || !turnManager.IsBattleActive)
        {
            value = turnManager != null && turnManager.BattleEnded
                ? "\u6218\u6597\u5df2\u7ed3\u675f" // 战斗已结束
                : "\u626b\u63cf\u5c01\u5996\u9635\u5f00\u59cb\u6218\u6597"; // 扫描封妖阵开始战斗
        }
        else if (!turnManager.IsPlayerTurn)
        {
            value = "\u5996\u602a\u884c\u52a8\u4e2d\n\u8bf7\u7a0d\u5019"; // 妖怪行动中 / 请稍候
        }
        else
        {
            WeaknessType w = intent != null ? intent.CurrentWeakness : WeaknessType.None;
            string tip = WeaknessCardTip(w);
            if (!string.IsNullOrEmpty(tip))
                value = tip + "\n\u62d6\u724c\u5bf9\u51c6\u5f31\u70b9\u53efQTE\n\u70b9\u51fb\u7ed3\u675f\u56de\u5408";
            // 拖牌对准弱点可QTE / 点击结束回合
            else
                value = "\u62d6\u51fa\u7b26\u5492\u653b\u51fb\u6216\u9632\u5fa1\n\u70b9\u51fb\u7ed3\u675f\u56de\u5408";
            // 拖出符咒攻击或防御 / 点击结束回合
        }

        if (!force && value == lastHintKey) return;
        lastHintKey = value;
        SetText(hintText, value, colorMuted);
    }

    private string BuildDeckLine()
    {
        if (cardDeck == null) return "";
        int left = cardDeck.DrawDeckCount;
        int hand = cardDeck.HandCount;
        int limit = cardDeck.HandLimit > 0 ? cardDeck.HandLimit : CardDeck.DefaultHandLimit;
        // 牌堆剩余N 手牌A/B
        return "\u724c\u5806\u5269\u4f59" + left + " \u624b\u724c" + hand + "/" + limit;
    }

    private static string WeaknessCardTip(WeaknessType type)
    {
        switch (type)
        {
            case WeaknessType.RedAttack:
                return "\u672c\u56de\u5408\u7ea2\u5f31\u70b9 \u7528\u65a9\u5996\u7b26"; // 本回合红弱点 用斩妖符
            case WeaknessType.YellowArmor:
                return "\u672c\u56de\u5408\u9ec4\u5f31\u70b9 \u7528\u7834\u715e\u7b26"; // 本回合黄弱点 用破煞符
            case WeaknessType.PurpleSeal:
                return "\u672c\u56de\u5408\u7d2b\u5f31\u70b9 \u7528\u9547\u9b42\u7b26"; // 本回合紫弱点 用镇魂符
            default:
                return "";
        }
    }

    private static void SetText(TextMeshProUGUI text, string value, Color color)
    {
        if (text == null) return;

        TmpChineseFontUtil.BindChineseFont(text);
        TmpChineseFontUtil.EnsureCharacters(text.font, value);

        text.richText = false;
        if (text.text != value)
            text.text = value;

        TmpChineseFontUtil.Apply(text, value);
        text.ForceMeshUpdate(true);

        if (text.color != color)
            text.color = color;
    }
}
