using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 胜负 / 流程提示面板。支持自定义按钮回调（继续下一关、重试等）。
/// </summary>
public class BattleResultUI : MonoBehaviour
{
    public TurnManager turnManager;
    public GameObject panelRoot;
    public TextMeshProUGUI resultText;
    public Button restartButton;
    public TextMeshProUGUI restartButtonLabel;

    public string winText = "封印成功！";
    public string loseText = "封印失败…";

    private bool subscribed;
    private Action pendingAction;
    private bool flowControlled; // 由 BattleFlowManager 接管时不自动弹默认胜负

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        // 有战役流程时，默认结果由 Flow 驱动
        flowControlled = FindAnyObjectByType<BattleFlowManager>() != null;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnPrimaryClick);
            restartButton.onClick.AddListener(OnPrimaryClick);
        }
        if (restartButtonLabel == null && restartButton != null)
            restartButtonLabel = restartButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void TrySubscribe()
    {
        if (subscribed) return;
        if (turnManager == null)
            turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();
        if (turnManager != null)
        {
            turnManager.OnBattleEnded += OnBattleEndedDefault;
            subscribed = true;
        }
    }

    private void OnDestroy()
    {
        if (turnManager != null && subscribed)
            turnManager.OnBattleEnded -= OnBattleEndedDefault;
    }

    private void OnBattleEndedDefault(bool playerWon)
    {
        // 有 BattleFlowManager 时不抢控制权
        if (FindAnyObjectByType<BattleFlowManager>() != null)
            return;

        ShowMessage(playerWon ? winText : loseText, true, "再战一次", () =>
        {
            HidePanel();
            turnManager?.RestartBattle();
        });
    }

    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        pendingAction = null;
    }

    /// <summary>
    /// 显示提示；showButton 为 false 时只显示文字（给奖励界面腾位置）
    /// </summary>
    public void ShowMessage(string message, bool showButton, string buttonLabel = "继续", Action onClick = null)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        if (resultText != null)
            resultText.text = message;

        pendingAction = onClick;

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(showButton);
            if (showButton && restartButtonLabel != null)
                restartButtonLabel.text = buttonLabel;
        }
    }

    private void OnPrimaryClick()
    {
        var action = pendingAction;
        pendingAction = null;
        if (action != null)
        {
            action.Invoke();
            return;
        }
        HidePanel();
        if (BattleFlowManager.Instance != null)
            BattleFlowManager.Instance.RetryCurrentStage();
        else if (turnManager != null)
            turnManager.RestartBattle();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
