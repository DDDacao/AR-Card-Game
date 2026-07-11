using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 玩家受击反馈：HUD 震动 + 全屏红闪。
/// </summary>
public class PlayerDamageFeedback : MonoBehaviour
{
    public static PlayerDamageFeedback Instance { get; private set; }

    [Header("引用（可空，自动查找）")]
    public CharacterStats playerStats;
    public Canvas targetCanvas;
    public Image redFlashImage;

    [Header("要抖动的 HUD 名（Canvas 子节点）")]
    public string[] shakeHudNames =
    {
        "HUD_Player", "HUD_Enemy", "HUD_SideInfo", "HUD_Actions"
    };

    [Header("震动（锚点像素）")]
    public float shakeDuration = 0.22f;
    public float shakeStrength = 42f;
    public float heavyShakeStrength = 64f;
    public int shakeVibrato = 22;
    public int heavyDamageThreshold = 8;

    [Header("红闪")]
    public Color flashColor = new Color(0.85f, 0.05f, 0.08f, 1f);
    public float flashPeakAlpha = 0.55f;
    public float heavyFlashPeakAlpha = 0.72f;
    public float flashIn = 0.06f;
    public float flashOut = 0.22f;

    private readonly List<RectTransform> shakeTargets = new List<RectTransform>();
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
            // 等几帧，保证 Canvas / Player 都已就绪
            if (frames < 2) return;
            EnsureExists();
            var fb = Instance;
            if (fb != null)
                fb.ForceRebind();
            // 多试几次，覆盖开战较晚的情况
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

        Canvas canvas = null;
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        // 优先 Screen Space 主 Canvas
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].gameObject.name == "Canvas")
            {
                canvas = canvases[i];
                break;
            }
        }
        if (canvas == null && canvases.Length > 0)
            canvas = canvases[0];
        if (canvas == null) return null;

        var host = canvas.gameObject.GetComponent<PlayerDamageFeedback>();
        if (host == null)
            host = canvas.gameObject.AddComponent<PlayerDamageFeedback>();
        host.targetCanvas = canvas;
        Instance = host;
        host.ForceRebind();
        return host;
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
        // 未绑定时周期性重试（开战前后 Player 引用才稳定）
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
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].name == "Canvas")
                {
                    targetCanvas = canvases[i];
                    break;
                }
            }
            if (targetCanvas == null && canvases.Length > 0)
                targetCanvas = canvases[0];
        }

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
        Debug.Log($"[PlayerDamageFeedback] 已绑定玩家受击：{boundStats.gameObject.name}");
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

        float strength = heavy ? heavyShakeStrength : shakeStrength;
        float dur = heavy ? shakeDuration * 1.2f : shakeDuration;

        for (int i = 0; i < shakeTargets.Count; i++)
        {
            var rt = shakeTargets[i];
            if (rt == null) continue;

            int id = rt.GetInstanceID();
            if (!basePos.ContainsKey(id))
                basePos[id] = rt.anchoredPosition;

            Vector2 origin = basePos[id];
            rt.DOKill();
            rt.anchoredPosition = origin;
            // 用显式 Vector2 强度，横纵都明显
            rt.DOShakeAnchorPos(dur, new Vector2(strength, strength * 0.65f), shakeVibrato, 90f, false, true)
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

        // 每次置顶，避免被其它 HUD 盖住
        redFlashImage.transform.SetAsLastSibling();
        redFlashImage.gameObject.SetActive(true);
        // 无 sprite 时 Unity UI Image 不绘制——强制白贴图
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
        // 用内置白贴图生成 Sprite（无 sprite 的 Image 不会画出来）
        var tex = Texture2D.whiteTexture;
        s_whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        s_whiteSprite.name = "PlayerDamageFeedback_White";
        return s_whiteSprite;
    }

    private void CollectShakeTargets()
    {
        shakeTargets.Clear();
        if (targetCanvas == null) return;

        for (int i = 0; i < shakeHudNames.Length; i++)
        {
            var t = targetCanvas.transform.Find(shakeHudNames[i]);
            if (t == null) continue;
            var rt = t as RectTransform;
            if (rt == null) continue;
            shakeTargets.Add(rt);
            int id = rt.GetInstanceID();
            if (!basePos.ContainsKey(id))
                basePos[id] = rt.anchoredPosition;
        }

        if (shakeTargets.Count == 0)
        {
            // 回退：抖整个 Canvas 根（除 flash）
            var rt = targetCanvas.transform as RectTransform;
            if (rt != null)
            {
                shakeTargets.Add(rt);
                int id = rt.GetInstanceID();
                if (!basePos.ContainsKey(id))
                    basePos[id] = rt.anchoredPosition;
            }
        }
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
