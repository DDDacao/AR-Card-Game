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

    private void Awake()
    {
        if (root == null) root = gameObject;
        if (tapButton != null)
        {
            tapButton.onClick.RemoveAllListeners();
            tapButton.onClick.AddListener(() => OnTap?.Invoke());
        }
        HideImmediate();
    }

    public void Show(int requiredClicks, float duration)
    {
        maxDuration = duration;
        if (root != null) root.SetActive(true);
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
