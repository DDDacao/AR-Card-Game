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
    [Tooltip("攻击状态墨迹底层（与结束回合按钮同系）；为空时自动加载 Resources/BattleHudSkin/intent_badge")]
    public Image intentBadgeBackground;
    public Sprite intentBadgeSprite;

    [Header("意图布局（手调优先）")]
    [Tooltip("关闭后：完全尊重场景里 RectTransform 手调，运行时只改文案/字体/贴图引用，不改 size/位置。\n开启后：按文案长度自动改文字框与墨迹尺寸（会覆盖你在 Hierarchy 的微调）。")]
    public bool autoFitIntentLayout = false;
    [Tooltip("相对文字框向外扩展（仅 autoFitIntentLayout=true 时生效）")]
    public Vector2 intentBadgePadMin = new Vector2(-48f, -18f);
    public Vector2 intentBadgePadMax = new Vector2(56f, 18f);
    [Tooltip("文字框相对 preferred 的内边距（仅 autoFit 时）")]
    public float intentTextPadX = 80f;
    public float intentTextPadY = 22f;
    public float intentMinWidth = 260f;
    public float intentMinHeight = 64f;
    public float intentMaxWidth = 720f;
    public float intentMaxHeight = 100f;
    [Tooltip("底图是否保持贴图宽高比（细长墨迹建议 false）")]
    public bool intentBadgePreserveAspect = false;

    [Header("样式")]
    [Tooltip("仅在父节点有非均匀缩放时，把意图/名称挪到 HUD 根下，避免被血条缩放压扁。")]
    public bool detachTextFromScaledParents = true;
    public Color colorIdle = new Color(0.96f, 0.94f, 0.90f, 1f);
    public Color colorPlayerTurn = new Color(0.98f, 0.90f, 0.55f, 1f);
    public Color colorEnemyTurn = new Color(1f, 0.62f, 0.48f, 1f);
    public Color colorIntent = new Color(1f, 0.94f, 0.82f, 1f);
    public Color colorMuted = new Color(0.88f, 0.86f, 0.80f, 0.96f);
    [Tooltip("墨迹底图着色（白=原色；可略偏金/红）")]
    public Color intentBadgeTint = Color.white;

    private TurnManager turnManager;
    private CardDeck cardDeck;
    private CharacterStats enemy;
    private EnemyIntentController intent;
    private float nextRefresh;
    private bool layoutPrepared;
    private string lastIntentKey;
    private string lastTurnKey;
    private string lastHintKey;

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
            // 只保证墨迹在文字下层 + 贴图引用；默认不改你手调的 Rect
            EnsureIntentBadgeBackground();
            if (autoFitIntentLayout)
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

        if (intentBadgeSprite == null)
            intentBadgeSprite = Resources.Load<Sprite>("BattleHudSkin/intent_badge");

        var textRt = enemyIntentText.rectTransform;
        Transform parent = textRt.parent;

        // 旧版把 Badge 挂在文字节点下：子 UI 后绘制会盖住父节点 TMP。必须改成「同级兄弟」。
        if (intentBadgeBackground == null)
        {
            Transform existing = null;
            if (parent != null)
                existing = parent.Find("IntentBadgeBg");
            if (existing == null)
                existing = textRt.Find("IntentBadgeBg");
            if (existing != null)
                intentBadgeBackground = existing.GetComponent<Image>();
        }

        if (intentBadgeBackground == null)
        {
            var go = new GameObject("IntentBadgeBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = enemyIntentText.gameObject.layer;
            go.transform.SetParent(parent != null ? parent : textRt, false);
            intentBadgeBackground = go.GetComponent<Image>();
            // 新建时才给一个合理默认尺寸；之后手调不被覆盖
            if (autoFitIntentLayout)
                SyncIntentBadgeToText();
        }

        // 若仍挂在文字下面，提到与文字同级（只改父节点，不改你手调的 size/pos）
        var bgRt = intentBadgeBackground.rectTransform;
        if (bgRt.parent == textRt && parent != null)
            bgRt.SetParent(parent, true); // worldPositionStays，尽量保留你摆好的位置

        // 贴图 / 着色：只在引用为空或需要刷新贴图时写入，不改 Rect
        if (intentBadgeSprite != null && intentBadgeBackground.sprite != intentBadgeSprite)
            intentBadgeBackground.sprite = intentBadgeSprite;
        intentBadgeBackground.type = Image.Type.Simple;
        intentBadgeBackground.preserveAspect = intentBadgePreserveAspect;
        intentBadgeBackground.color = intentBadgeTint;
        intentBadgeBackground.raycastTarget = false;

        // 绘制顺序：墨迹在下，文字在上（只调 sibling，不改尺寸）
        if (bgRt.parent == textRt.parent && textRt.parent != null)
        {
            if (textRt.GetSiblingIndex() <= bgRt.GetSiblingIndex())
                textRt.SetSiblingIndex(bgRt.GetSiblingIndex() + 1);
        }

        if (autoFitIntentLayout)
            SyncIntentBadgeToText();
    }

    /// <summary>
    /// 仅 autoFitIntentLayout 时使用：墨迹对齐文字并按边距扩尺寸。
    /// </summary>
    private void SyncIntentBadgeToText()
    {
        if (!autoFitIntentLayout) return;
        if (intentBadgeBackground == null || enemyIntentText == null) return;

        var textRt = enemyIntentText.rectTransform;
        var bgRt = intentBadgeBackground.rectTransform;

        bgRt.localScale = Vector3.one;
        bgRt.anchorMin = textRt.anchorMin;
        bgRt.anchorMax = textRt.anchorMax;
        bgRt.pivot = textRt.pivot;

        float padL = Mathf.Abs(intentBadgePadMin.x);
        float padB = Mathf.Abs(intentBadgePadMin.y);
        float padR = Mathf.Abs(intentBadgePadMax.x);
        float padT = Mathf.Abs(intentBadgePadMax.y);

        bgRt.sizeDelta = textRt.sizeDelta + new Vector2(padL + padR, padB + padT);
        float dx = (padR - padL) * 0.5f;
        float dy = (padT - padB) * 0.5f;
        bgRt.anchoredPosition = textRt.anchoredPosition + new Vector2(dx, dy);

        intentBadgeBackground.color = intentBadgeTint;
    }

    /// <summary>
    /// 仅 autoFitIntentLayout 时使用：按文案 preferred 改文字框宽高。
    /// </summary>
    private void FitIntentBoxToText()
    {
        if (!autoFitIntentLayout) return;
        if (enemyIntentText == null) return;

        enemyIntentText.ForceMeshUpdate(true);
        float w = enemyIntentText.preferredWidth + intentTextPadX;
        float h = enemyIntentText.preferredHeight + intentTextPadY;

        if (w < intentMinWidth) w = intentMinWidth;
        if (h < intentMinHeight) h = intentMinHeight;
        w = Mathf.Min(w, intentMaxWidth);
        h = Mathf.Min(h, intentMaxHeight);

        var rt = enemyIntentText.rectTransform;
        rt.sizeDelta = new Vector2(w, h);

        SyncIntentBadgeToText();

        if (intentBadgeBackground != null
            && intentBadgeBackground.transform.parent == rt.parent
            && rt.parent != null)
        {
            var bgRt = intentBadgeBackground.rectTransform;
            if (rt.GetSiblingIndex() <= bgRt.GetSiblingIndex())
                rt.SetSiblingIndex(bgRt.GetSiblingIndex() + 1);
        }
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

        EnsureIntentBadgeBackground();
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
            // 本回合弱点已击破：提示不可再打
            if (intent != null && intent.WeaknessExpendedThisTurn && intent.PlannedWeakness != WeaknessType.None)
            {
                value = "\u672c\u56de\u5408\u5f31\u70b9\u5df2\u51fb\u7834\n\u53ef\u7ee7\u7eed\u51fa\u724c\u6216\u7ed3\u675f\u56de\u5408";
                // 本回合弱点已击破 / 可继续出牌或结束回合
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
