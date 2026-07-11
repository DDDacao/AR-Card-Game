using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 连续点击 QTE：默认 2 秒内点 3 次。
/// </summary>
public class QTEManager : MonoBehaviour
{
    public static QTEManager Instance { get; private set; }

    [Header("默认参数")]
    public float defaultDuration = 2f;
    public int defaultRequiredClicks = 3;

    [Header("UI")]
    public QTEPanelUI panelUI;

    public bool IsRunning { get; private set; }

    private Action<bool> onComplete;
    private int requiredClicks;
    private int currentClicks;
    private float timeLeft;
    private Coroutine runRoutine;

    private void Awake()
    {
        Instance = this;
        if (panelUI == null)
            panelUI = FindAnyObjectByType<QTEPanelUI>(FindObjectsInactive.Include);
        if (panelUI != null)
            panelUI.HideImmediate();
    }

    /// <summary>
    /// 开始 QTE。完成后回调 success。
    /// </summary>
    public void StartClickQTE(Action<bool> complete, float duration = -1f, int needClicks = -1)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[QTEManager] 已有 QTE 进行中，忽略新请求。");
            return;
        }

        if (duration <= 0f) duration = defaultDuration;
        if (needClicks <= 0) needClicks = defaultRequiredClicks;

        onComplete = complete;
        requiredClicks = needClicks;
        currentClicks = 0;
        timeLeft = duration;
        IsRunning = true;

        if (panelUI == null)
            panelUI = FindAnyObjectByType<QTEPanelUI>(FindObjectsInactive.Include);

        if (panelUI != null)
        {
            // 先绑回调再 Show，避免激活瞬间点击漏事件（极少见）
            panelUI.OnTap = RegisterClick;
            panelUI.Show(requiredClicks, duration);
        }
        else
        {
            Debug.LogWarning("[QTEManager] 无 QTEPanelUI，自动判定成功。");
            Finish(true);
            return;
        }

        if (runRoutine != null) StopCoroutine(runRoutine);
        runRoutine = StartCoroutine(RunTimer());
    }

    public void RegisterClick()
    {
        if (!IsRunning) return;

        currentClicks++;
        if (panelUI != null)
            panelUI.SetProgress(currentClicks, requiredClicks, timeLeft);

        if (currentClicks >= requiredClicks)
            Finish(true);
    }

    private IEnumerator RunTimer()
    {
        while (timeLeft > 0f && IsRunning)
        {
            timeLeft -= Time.deltaTime;
            if (panelUI != null)
                panelUI.SetProgress(currentClicks, requiredClicks, Mathf.Max(0f, timeLeft));
            yield return null;
        }

        if (IsRunning)
            Finish(false);
    }

    private void Finish(bool success)
    {
        if (!IsRunning && onComplete == null) return;

        IsRunning = false;
        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
            runRoutine = null;
        }

        if (panelUI != null)
        {
            panelUI.OnTap = null;
            panelUI.ShowResult(success);
        }

        var cb = onComplete;
        onComplete = null;
        cb?.Invoke(success);
    }
}
