using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 镇魂铃 QTE：限时连敲铃铛，带抖动与波纹反馈。
/// 第三击（收定）有更强反馈。
/// </summary>
public class QTEPanelUI : MonoBehaviour
{
    [Header("根节点（默认本物体）")]
    public GameObject root;

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI resultText;
    public Button tapButton;
    public Image fillBar;
    public Slider timeSlider;

    [Header("铃铛反馈")]
    public RectTransform bellRoot;
    public Image bellImage;
    public RectTransform rippleRoot;
    public Sprite rippleSprite;
    public Color rippleColor = new Color(1f, 0.85f, 0.35f, 0.85f);
    public float punchRotationZ = 14f;
    public float punchScale = 0.12f;
    public float punchDuration = 0.16f;
    public float rippleDuration = 0.42f;
    public float rippleEndScale = 2.2f;
    public int maxActiveRipples = 6;

    [Header("结果展示时长")]
    public float successHideDelay = 0.85f;
    public float failHideDelay = 0.55f;

    public Action OnTap;
    /// <summary>结果展示结束、面板隐藏后回调（成功结算应挂这里）。</summary>
    public Action OnHiddenAfterResult;

    private float maxDuration = 2f;
    private int requiredClicksCached = 3;
    private bool buttonWired;
    private bool finisherPlayed;
    private Vector3 bellBaseScale = Vector3.one;
    private Quaternion bellBaseRotation = Quaternion.identity;
    private readonly List<GameObject> activeRipples = new List<GameObject>();
    private Sequence resultSeq;
    private Sequence textSeq;
    private Sequence progressFlashSeq;

    private void Awake()
    {
        EnsureInitialized();
        // 不要在 Awake 里 HideImmediate：HUD_QTE 默认 inactive，
        // 首次 Show 激活时才会跑 Awake，若立刻 Hide 会吞掉第一次 QTE。
    }

    private void OnDisable()
    {
        KillBellTweens();
        ClearRipples();
        resultSeq?.Kill();
        resultSeq = null;
        textSeq?.Kill();
        textSeq = null;
        progressFlashSeq?.Kill();
        progressFlashSeq = null;
    }

    private void EnsureInitialized()
    {
        if (root == null) root = gameObject;

        if (bellRoot != null)
        {
            bellBaseScale = bellRoot.localScale;
            bellBaseRotation = bellRoot.localRotation;
        }

        if (buttonWired || tapButton == null) return;

        tapButton.onClick.RemoveAllListeners();
        tapButton.onClick.AddListener(HandleTap);
        buttonWired = true;
    }

    private void HandleTap()
    {
        // 反馈由 QTEManager 在计数后调用 PlayClickFeedback，
        // 以便第三击能拿到正确的 click 序号。
        OnTap?.Invoke();
    }

    public void Show(int requiredClicks, float duration)
    {
        EnsureInitialized();
        maxDuration = duration;
        requiredClicksCached = Mathf.Max(1, requiredClicks);
        finisherPlayed = false;

        CancelInvoke(nameof(HideAfterResult));
        OnHiddenAfterResult = null;
        resultSeq?.Kill();
        resultSeq = null;
        textSeq?.Kill();
        textSeq = null;
        progressFlashSeq?.Kill();
        progressFlashSeq = null;
        KillBellTweens();
        ClearRipples();
        ResetBellTransform();

        if (root != null) root.SetActive(true);
        EnsureInitialized();

        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
            resultText.text = "";
            var rt = resultText.rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
            var c = resultText.color;
            c.a = 1f;
            resultText.color = c;
        }
        if (titleText != null)
            titleText.text = "命中破绽！连敲镇魂铃";
        if (tapButton != null)
            tapButton.interactable = true;

        if (bellImage != null)
            bellImage.color = Color.white;

        if (progressText != null)
        {
            progressText.transform.localScale = Vector3.one;
            progressText.color = new Color(1f, 0.9f, 0.55f, 1f);
        }

        SetProgress(0, requiredClicksCached, duration);
    }

    public void SetProgress(int clicks, int required, float timeLeft)
    {
        if (progressText != null)
            progressText.text = $"{clicks} / {required}";
        if (timerText != null)
            timerText.text = $"{Mathf.Max(0f, timeLeft):0.0}s";
        if (timeSlider != null)
        {
            timeSlider.maxValue = maxDuration;
            timeSlider.value = Mathf.Clamp(timeLeft, 0f, maxDuration);
        }
        if (fillBar != null && maxDuration > 0f)
            fillBar.fillAmount = Mathf.Clamp01(timeLeft / maxDuration);
    }

    /// <summary>
    /// 第 n 次点击反馈。最后一击走收定特效。
    /// </summary>
    public void PlayClickFeedback(int clicks, int required)
    {
        if (clicks <= 0) return;
        bool isFinisher = required > 0 && clicks >= required;
        if (isFinisher)
            PlayFinisherFeedback();
        else
            PlayNormalTapFeedback(clicks, required);
    }

    public void ShowResult(bool success)
    {
        if (tapButton != null)
            tapButton.interactable = false;

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = success ? "镇魂成功！" : "铃音未成…";
            resultText.color = success
                ? new Color(1f, 0.92f, 0.45f, 1f)
                : new Color(1f, 0.45f, 0.4f, 1f);
        }

        if (success)
        {
            // 若第三击已播过收定，这里补文案上浮与轻闪；否则完整成功反馈
            if (!finisherPlayed)
                PlayResultFeedback(true);
            else
                PlaySuccessTextPop();
        }
        else
        {
            PlayResultFeedback(false);
        }

        CancelInvoke(nameof(HideAfterResult));
        float delay = success ? successHideDelay : failHideDelay;
        Invoke(nameof(HideAfterResult), delay);
    }

    /// <summary>结果展示剩余/设定隐藏延迟（供外部对齐结算时机）。</summary>
    public float GetResultHideDelay(bool success) => success ? successHideDelay : failHideDelay;

    private void HideAfterResult()
    {
        HideImmediate();
        var cb = OnHiddenAfterResult;
        OnHiddenAfterResult = null;
        cb?.Invoke();
    }

    public void HideImmediate()
    {
        CancelInvoke(nameof(HideAfterResult));
        KillBellTweens();
        ClearRipples();
        resultSeq?.Kill();
        resultSeq = null;
        textSeq?.Kill();
        textSeq = null;
        progressFlashSeq?.Kill();
        progressFlashSeq = null;
        ResetBellTransform();
        if (root != null) root.SetActive(false);
    }

    private void PlayNormalTapFeedback(int clicks, int required)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBellClick();
        }

        if (bellRoot != null)
        {
            KillBellTweens();
            bellRoot.localScale = bellBaseScale;
            bellRoot.localRotation = bellBaseRotation;

            // 越接近收定，抖动略增强
            float t = required > 1 ? (clicks - 1f) / (required - 1f) : 0f;
            float rot = punchRotationZ * (1f + t * 0.25f);
            float scale = punchScale * (1f + t * 0.2f);

            bellRoot.DOPunchRotation(new Vector3(0f, 0f, rot), punchDuration, 10, 0.6f)
                .SetUpdate(true);
            bellRoot.DOPunchScale(Vector3.one * scale, punchDuration, 8, 0.55f)
                .SetUpdate(true);
        }

        SpawnRipple(1f + (clicks - 1) * 0.08f);
    }

    private void PlayFinisherFeedback()
    {
        finisherPlayed = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBellClick();
        }

        if (bellRoot != null)
        {
            KillBellTweens();
            bellRoot.localScale = bellBaseScale;
            bellRoot.localRotation = bellBaseRotation;

            resultSeq?.Kill();
            resultSeq = DOTween.Sequence().SetUpdate(true);
            resultSeq.Append(bellRoot.DOPunchRotation(new Vector3(0f, 0f, punchRotationZ * 1.8f), 0.22f, 12, 0.55f));
            resultSeq.Join(bellRoot.DOPunchScale(Vector3.one * (punchScale * 2.2f), 0.28f, 10, 0.45f));
            if (bellImage != null)
            {
                var baseCol = Color.white;
                bellImage.color = baseCol;
                resultSeq.Join(bellImage.DOColor(new Color(1f, 0.95f, 0.45f, 1f), 0.1f));
                resultSeq.Append(bellImage.DOColor(baseCol, 0.35f));
            }
        }

        // 大波纹 ×3
        SpawnRipple(1.25f, new Color(1f, 0.9f, 0.35f, 0.95f), 0.55f, 2.8f);
        SpawnRipple(1.55f, new Color(1f, 0.82f, 0.25f, 0.85f), 0.62f, 3.2f);
        SpawnRipple(1.9f, new Color(1f, 0.75f, 0.2f, 0.7f), 0.7f, 3.6f);

        FlashProgressFull();
    }

    private void FlashProgressFull()
    {
        if (progressText == null) return;
        progressFlashSeq?.Kill();
        progressText.transform.localScale = Vector3.one;
        progressText.color = new Color(1f, 0.95f, 0.4f, 1f);
        progressFlashSeq = DOTween.Sequence().SetUpdate(true);
        progressFlashSeq.Append(progressText.transform.DOPunchScale(Vector3.one * 0.35f, 0.28f, 8, 0.5f));
        progressFlashSeq.Join(progressText.DOColor(new Color(1f, 1f, 0.75f, 1f), 0.12f));
        progressFlashSeq.Append(progressText.DOColor(new Color(1f, 0.9f, 0.55f, 1f), 0.25f));
    }

    private void PlaySuccessTextPop()
    {
        if (resultText == null) return;

        // 独立序列，避免打断第三击铃铛收定动画
        textSeq?.Kill();
        var rt = resultText.rectTransform;
        float baseY = 40f;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, baseY - 30f);
        resultText.transform.localScale = Vector3.one * 0.6f;
        var col = resultText.color;
        col.a = 0f;
        resultText.color = col;

        textSeq = DOTween.Sequence().SetUpdate(true);
        textSeq.Append(resultText.transform.DOScale(1.15f, 0.16f).SetEase(Ease.OutBack));
        textSeq.Join(rt.DOAnchorPosY(baseY + 50f, 0.55f).SetEase(Ease.OutCubic));
        textSeq.Join(resultText.DOFade(1f, 0.1f));
        textSeq.Append(resultText.transform.DOScale(1f, 0.08f));
        textSeq.AppendInterval(0.12f);
        textSeq.Append(resultText.DOFade(0f, 0.28f));
    }

    private void PlayResultFeedback(bool success)
    {
        if (bellRoot == null)
        {
            if (success) PlaySuccessTextPop();
            return;
        }

        KillBellTweens();
        ResetBellTransform();

        resultSeq = DOTween.Sequence().SetUpdate(true);
        if (success)
        {
            resultSeq.Append(bellRoot.DOPunchScale(Vector3.one * 0.22f, 0.28f, 8, 0.5f));
            if (bellImage != null)
            {
                var c = bellImage.color;
                resultSeq.Join(bellImage.DOColor(new Color(1f, 0.95f, 0.55f, 1f), 0.12f));
                resultSeq.Append(bellImage.DOColor(c, 0.25f));
            }
            SpawnRipple(1.15f);
            SpawnRipple(1.35f);
            PlaySuccessTextPop();
        }
        else
        {
            resultSeq.Append(bellRoot.DOShakeRotation(0.28f, new Vector3(0f, 0f, 18f), 14, 90f, false));
            if (bellImage != null)
                resultSeq.Join(bellImage.DOColor(new Color(0.55f, 0.55f, 0.58f, 1f), 0.2f));
        }
    }

    private void SpawnRipple(float scaleMul = 1f)
    {
        SpawnRipple(scaleMul, rippleColor, rippleDuration, rippleEndScale);
    }

    private void SpawnRipple(float scaleMul, Color color, float duration, float endScale)
    {
        Transform parent = rippleRoot != null
            ? rippleRoot
            : (bellRoot != null ? bellRoot : transform);

        while (activeRipples.Count >= maxActiveRipples)
        {
            var old = activeRipples[0];
            activeRipples.RemoveAt(0);
            if (old != null) Destroy(old);
        }

        var go = new GameObject("Ripple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        float startSize = 80f * scaleMul;
        rt.sizeDelta = new Vector2(startSize, startSize);
        rt.localScale = Vector3.one * 0.35f;
        rt.SetAsFirstSibling();

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = color;
        if (rippleSprite != null)
        {
            img.sprite = rippleSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }

        activeRipples.Add(go);

        float end = endScale * scaleMul;
        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Join(rt.DOScale(end, duration).SetEase(Ease.OutCubic));
        seq.Join(img.DOFade(0f, duration).SetEase(Ease.OutQuad));
        seq.OnComplete(() =>
        {
            activeRipples.Remove(go);
            if (go != null) Destroy(go);
        });
    }

    private void KillBellTweens()
    {
        if (bellRoot != null)
            bellRoot.DOKill();
        if (bellImage != null)
            bellImage.DOKill();
    }

    private void ResetBellTransform()
    {
        if (bellRoot == null) return;
        bellRoot.localScale = bellBaseScale;
        bellRoot.localRotation = bellBaseRotation;
    }

    private void ClearRipples()
    {
        for (int i = 0; i < activeRipples.Count; i++)
        {
            if (activeRipples[i] != null)
            {
                activeRipples[i].transform.DOKill();
                Destroy(activeRipples[i]);
            }
        }
        activeRipples.Clear();
    }
}
