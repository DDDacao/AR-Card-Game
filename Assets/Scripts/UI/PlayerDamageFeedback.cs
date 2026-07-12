using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 玩家受击反馈：新版 HUD 震动 + 全屏红闪。
/// 优先抖动 HUD_ArtSkin_Adjustable（及玩家侧关键节点），兼容旧 HUD 名。
/// </summary>
public class PlayerDamageFeedback : MonoBehaviour
{
    public static PlayerDamageFeedback Instance { get; private set; }

    [Header("引用（可空，自动查找）")]
    public CharacterStats playerStats;
    public Canvas targetCanvas;
    public Image redFlashImage;

    [Header("要抖动的 HUD（按优先级收集，存在即加入）")]
    [Tooltip("新版可调 HUD 根节点，最重要")]
    public string primaryHudRoot = "HUD_ArtSkin_Adjustable";

    [Tooltip("优先抖动的子节点名（在 primaryHudRoot 下查找）")]
    public string[] preferredChildNames =
    {
        "PlayerHealth_可调",
        "PlayerEnergy_可调",
        "PlayerArmor_可调",
        "EndTurn_Adjustable",
        "TurnInfoPanel_可调",
        "BossHealth_可调"
    };

    [Tooltip("旧版 HUD 名（仅作兼容回退）")]
    public string[] legacyHudNames =
    {
        "HUD_Player", "HUD_Enemy", "HUD_SideInfo", "HUD_Actions"
    };

    [Header("震动（锚点像素）")]
    public float shakeDuration = 0.28f;
    public float shakeStrength = 28f;
    public float heavyShakeStrength = 48f;
    public int shakeVibrato = 24;
    public int heavyDamageThreshold = 8;
    [Tooltip("主 HUD 根额外放大一点强度")]
    public float rootStrengthMul = 1.15f;

    [Header("红闪")]
    public Color flashColor = new Color(0.85f, 0.05f, 0.08f, 1f);
    public float flashPeakAlpha = 0.48f;
    public float heavyFlashPeakAlpha = 0.68f;
    public float flashIn = 0.05f;
    public float flashOut = 0.28f;

    private readonly List<RectTransform> shakeTargets = new List<RectTransform>();
    private readonly HashSet<int> shakeTargetIds = new HashSet<int>();
    private readonly Dictionary<int, Vector2> basePos = new Dictionary<int, Vector2>();
    private Tween flashTween;
    private CharacterStats boundStats;
    private static Sprite s_whiteSprite;
    private float rebindTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoEnsure()
    {
        var go = new GameObject("_PlayerDamageFeedbackBootstrap");
        DontDestroyOnLoad(go);
        go.AddComponent<Bootstrap>();
    }

    private sealed class Bootstrap : MonoBehaviour
    {
        private int frames;

        private void Update()
        {
            frames++;
            if (frames < 2) return;
            EnsureExists();
            var fb = Instance;
            if (fb != null)
                fb.ForceRebind();
            if (frames > 120 || (fb != null && fb.IsBound))
                Destroy(gameObject);
        }
    }

    public bool IsBound => boundStats != null;

    public static PlayerDamageFeedback EnsureExists()
    {
        if (Instance != null)
        {
            Instance.ForceRebind();
            return Instance;
        }

        var existing = FindAnyObjectByType<PlayerDamageFeedback>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            existing.ForceRebind();
            return existing;
        }

        Canvas canvas = FindMainCanvas();
        if (canvas == null) return null;

        var host = canvas.gameObject.GetComponent<PlayerDamageFeedback>();
        if (host == null)
            host = canvas.gameObject.AddComponent<PlayerDamageFeedback>();
        host.targetCanvas = canvas;
        Instance = host;
        host.ForceRebind();
        return host;
    }

    private static Canvas FindMainCanvas()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].gameObject.name == "Canvas")
                return canvases[i];
        }
        return canvases != null && canvases.Length > 0 ? canvases[0] : null;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ForceRebind();
    }

    private void Update()
    {
        if (boundStats != null) return;
        rebindTimer -= Time.unscaledDeltaTime;
        if (rebindTimer > 0f) return;
        rebindTimer = 0.5f;
        ForceRebind();
    }

    private void OnDestroy()
    {
        Unbind();
        if (Instance == this) Instance = null;
        flashTween?.Kill();
        ResetShakeTargets();
    }

    public void ForceRebind()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();
        if (targetCanvas == null)
            targetCanvas = FindMainCanvas();

        EnsureFlashOverlay();
        CollectShakeTargets();

        CharacterStats found = null;
        var tm = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();
        if (tm != null)
        {
            tm.ResolveReferences();
            found = tm.playerStats;
        }
        if (found == null)
        {
            var p = GameObject.Find("Player");
            if (p == null) p = GameObject.Find("PlayerManager");
            if (p != null) found = p.GetComponent<CharacterStats>();
        }

        if (found == null) return;
        if (boundStats == found) return;

        Unbind();
        playerStats = found;
        boundStats = found;
        boundStats.OnTookHit += OnPlayerTookHit;
        Debug.Log($"[PlayerDamageFeedback] 已绑定玩家受击：{boundStats.gameObject.name}，抖动节点={shakeTargets.Count}");
    }

    private void Unbind()
    {
        if (boundStats != null)
            boundStats.OnTookHit -= OnPlayerTookHit;
        boundStats = null;
    }

    private void OnPlayerTookHit(int incomingDamage)
    {
        Play(incomingDamage);
    }

    /// <summary>外部也可手动调用。</summary>
    public void Play(int incomingDamage)
    {
        if (incomingDamage <= 0) return;

        if (targetCanvas == null)
            ForceRebind();
        EnsureFlashOverlay();
        CollectShakeTargets();

        bool heavy = incomingDamage >= heavyDamageThreshold;
        Debug.Log($"[PlayerDamageFeedback] 受击反馈 dmg={incomingDamage} heavy={heavy} shakeN={shakeTargets.Count} flash={(redFlashImage != null)}");

        PlayShake(heavy);
        PlayRedFlash(heavy);
    }

    private void PlayShake(bool heavy)
    {
        if (shakeTargets.Count == 0)
            CollectShakeTargets();
        if (shakeTargets.Count == 0) return;

        float strength = heavy ? heavyShakeStrength : shakeStrength;
        float dur = heavy ? shakeDuration * 1.25f : shakeDuration;

        for (int i = 0; i < shakeTargets.Count; i++)
        {
            var rt = shakeTargets[i];
            if (rt == null) continue;

            int id = rt.GetInstanceID();
            if (!basePos.ContainsKey(id))
                basePos[id] = rt.anchoredPosition;

            Vector2 origin = basePos[id];
            // 根节点稍强一点
            float mul = (rt.name == primaryHudRoot) ? rootStrengthMul : 1f;
            float s = strength * mul;

            rt.DOKill(true);
            rt.anchoredPosition = origin;
            rt.DOShakeAnchorPos(dur, new Vector2(s, s * 0.7f), shakeVibrato, 90f, false, true)
                .SetUpdate(true)
                .SetId("PlayerDmgShake_" + id)
                .OnKill(() =>
                {
                    if (rt != null) rt.anchoredPosition = origin;
                })
                .OnComplete(() =>
                {
                    if (rt != null) rt.anchoredPosition = origin;
                });
        }
    }

    private void PlayRedFlash(bool heavy)
    {
        EnsureFlashOverlay();
        if (redFlashImage == null) return;

        redFlashImage.transform.SetAsLastSibling();
        redFlashImage.gameObject.SetActive(true);
        if (redFlashImage.sprite == null)
            redFlashImage.sprite = GetWhiteSprite();

        flashTween?.Kill();
        var clear = flashColor;
        clear.a = 0f;
        redFlashImage.color = clear;

        float peak = heavy ? heavyFlashPeakAlpha : flashPeakAlpha;
        var peakColor = flashColor;
        peakColor.a = peak;

        flashTween = DOTween.Sequence().SetUpdate(true)
            .Append(redFlashImage.DOColor(peakColor, flashIn).SetEase(Ease.OutQuad))
            .Append(redFlashImage.DOColor(clear, flashOut).SetEase(Ease.InQuad));
    }

    private void EnsureFlashOverlay()
    {
        if (targetCanvas == null) return;

        if (redFlashImage != null)
        {
            if (redFlashImage.sprite == null)
                redFlashImage.sprite = GetWhiteSprite();
            return;
        }

        var existing = targetCanvas.transform.Find("HUD_DamageFlash");
        if (existing != null)
        {
            redFlashImage = existing.GetComponent<Image>();
            if (redFlashImage == null)
                redFlashImage = existing.gameObject.AddComponent<Image>();
            redFlashImage.sprite = GetWhiteSprite();
            redFlashImage.raycastTarget = false;
            return;
        }

        var go = new GameObject("HUD_DamageFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(targetCanvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        redFlashImage = go.GetComponent<Image>();
        redFlashImage.sprite = GetWhiteSprite();
        redFlashImage.type = Image.Type.Simple;
        redFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        redFlashImage.raycastTarget = false;
        go.transform.SetAsLastSibling();
    }

    private static Sprite GetWhiteSprite()
    {
        if (s_whiteSprite != null) return s_whiteSprite;
        var tex = Texture2D.whiteTexture;
        s_whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        s_whiteSprite.name = "PlayerDamageFeedback_White";
        return s_whiteSprite;
    }

    private void CollectShakeTargets()
    {
        shakeTargets.Clear();
        shakeTargetIds.Clear();
        if (targetCanvas == null) return;

        Transform canvasTf = targetCanvas.transform;

        // 1) 新版 HUD 根
        Transform artRoot = canvasTf.Find(primaryHudRoot);
        if (artRoot == null)
        {
            // 深度找一次（防止改名层级）
            artRoot = FindDeep(canvasTf, primaryHudRoot);
        }
        if (artRoot != null)
        {
            TryAddShake(artRoot as RectTransform);

            // 2) 关键关键的玩家侧 / 关键 UI 子节点（更有「受击」感）
            for (int i = 0; i < preferredChildNames.Length; i++)
            {
                var child = artRoot.Find(preferredChildNames[i]);
                if (child == null)
                    child = FindDeep(artRoot, preferredChildNames[i]);
                TryAddShake(child as RectTransform);
            }
        }

        // 3) 旧版 HUD 兼容
        for (int i = 0; i < legacyHudNames.Length; i++)
        {
            var t = canvasTf.Find(legacyHudNames[i]);
            if (t != null && t.gameObject.activeInHierarchy)
                TryAddShake(t as RectTransform);
        }

        // 4) 仍没有：抖 Canvas 根（最后手段）
        if (shakeTargets.Count == 0)
            TryAddShake(canvasTf as RectTransform);
    }

    private void TryAddShake(RectTransform rt)
    {
        if (rt == null) return;
        // 不抖红闪层
        if (rt.name == "HUD_DamageFlash") return;
        int id = rt.GetInstanceID();
        if (!shakeTargetIds.Add(id)) return;
        shakeTargets.Add(rt);
        if (!basePos.ContainsKey(id))
            basePos[id] = rt.anchoredPosition;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var f = FindDeep(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }

    private void ResetShakeTargets()
    {
        for (int i = 0; i < shakeTargets.Count; i++)
        {
            var rt = shakeTargets[i];
            if (rt == null) continue;
            rt.DOKill();
            int id = rt.GetInstanceID();
            if (basePos.TryGetValue(id, out var p))
                rt.anchoredPosition = p;
        }
    }
}
