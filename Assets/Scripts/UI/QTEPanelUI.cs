using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 全屏半透明 QTE：大按钮连续点击
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
    public Image fillBar; // 可选：时间条
    public Slider timeSlider;

    public Action OnTap;

    private float maxDuration = 2f;
    private bool buttonWired;

    private void Awake()
    {
        EnsureInitialized();
        // 注意：不要在 Awake 里 HideImmediate。
        // HUD_QTE 默认 inactive，首次 Show() 激活时才会跑 Awake；
        // 若此处立刻 Hide，第一次 QTE 界面会刚弹出就被关掉。
        // 初始隐藏由场景默认状态 / QTEManager.Awake 负责。
    }

    private void EnsureInitialized()
    {
        if (root == null) root = gameObject;
        if (buttonWired || tapButton == null) return;

        tapButton.onClick.RemoveAllListeners();
        tapButton.onClick.AddListener(() => OnTap?.Invoke());
        buttonWired = true;
    }

    public void Show(int requiredClicks, float duration)
    {
        EnsureInitialized();
        maxDuration = duration;

        // 取消可能残留的结果延迟隐藏，避免第一次 QTE 被旧 Invoke 冲掉
        CancelInvoke(nameof(HideImmediate));

        if (root != null) root.SetActive(true);

        // 若 root 就是本物体，SetActive 可能刚触发 Awake；再确保按钮已绑
        EnsureInitialized();

        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
            resultText.text = "";
        }
        if (titleText != null)
            titleText.text = "命中破绽！快速点击封印";
        SetProgress(0, requiredClicks, duration);
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

    public void ShowResult(bool success)
    {
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = success ? "封印成功！" : "封印失败…";
            resultText.color = success
                ? new Color(0.4f, 1f, 0.5f)
                : new Color(1f, 0.4f, 0.35f);
        }
        // 短延迟后隐藏
        CancelInvoke(nameof(HideImmediate));
        Invoke(nameof(HideImmediate), 0.55f);
    }

    public void HideImmediate()
    {
        if (root != null) root.SetActive(false);
    }
}
