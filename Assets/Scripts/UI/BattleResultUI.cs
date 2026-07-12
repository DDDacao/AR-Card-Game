using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 胜负 / 流程提示面板。
/// 胜利时使用双层美术（VictorySkin）；失败/通用提示走旧 Message 面板。
/// </summary>
public class BattleResultUI : MonoBehaviour
{
    public TurnManager turnManager;
    public GameObject panelRoot;
    public TextMeshProUGUI resultText;
    public Button restartButton;
    public TextMeshProUGUI restartButtonLabel;

    [Header("胜利美术（可空，自动查找）")]
    public BattleResultVictorySkin victorySkin;
    [Tooltip("非胜利时显示的旧式消息框（可空）")]
    public GameObject messagePanel;

    public string winText = "封印成功！";
    public string loseText = "封印失败…";

    private bool subscribed;
    private Action pendingAction;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        AutoBind();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        AutoBind();
        TrySubscribe();
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnPrimaryClick);
            restartButton.onClick.AddListener(OnPrimaryClick);
        }
        if (restartButtonLabel == null && restartButton != null)
            restartButtonLabel = restartButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void AutoBind()
    {
        if (victorySkin == null)
            victorySkin = GetComponentInChildren<BattleResultVictorySkin>(true);
        if (victorySkin == null && panelRoot != null)
            victorySkin = panelRoot.GetComponentInChildren<BattleResultVictorySkin>(true);

        if (messagePanel == null && panelRoot != null)
        {
            var t = panelRoot.transform.Find("MessagePanel_可调");
            if (t == null) t = panelRoot.transform.Find("Panel");
            if (t != null) messagePanel = t.gameObject;
        }
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

        if (playerWon)
        {
            ShowVictory("", true, "再战一次", () =>
            {
                HidePanel();
                turnManager?.RestartBattle();
            });
        }
        else
        {
            ShowMessage(loseText, true, "再战一次", () =>
            {
                HidePanel();
                turnManager?.RestartBattle();
            });
        }
    }

    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        if (victorySkin != null)
            victorySkin.ShowSkin(false);
        pendingAction = null;
    }

    /// <summary>
    /// 显示战斗胜利美术（底层 + 战斗胜利字体）+ 可选副标题与按钮。
    /// </summary>
    public void ShowVictory(string detailMessage, bool showButton, string buttonLabel = "继续", Action onClick = null)
    {
        AutoBind();

        if (panelRoot != null)
            panelRoot.SetActive(true);

        // 隐藏旧消息框
        if (messagePanel != null)
            messagePanel.SetActive(false);

        if (victorySkin != null)
        {
            victorySkin.ShowSkin(true);
            // 去掉重复的「封印成功」标题行，只留引导文案
            string detail = SanitizeVictoryDetail(detailMessage);
            victorySkin.SetDetail(detail);
        }
        else if (resultText != null)
        {
            // 无美术时回退
            string fallback = string.IsNullOrWhiteSpace(detailMessage)
                ? winText
                : (detailMessage.Contains("封印") || detailMessage.Contains("胜利")
                    ? detailMessage
                    : winText + "\n" + detailMessage);
            resultText.text = fallback;
            TmpChineseFontUtil.Apply(resultText, fallback);
            if (messagePanel != null)
                messagePanel.SetActive(true);
        }

        pendingAction = onClick;
        ApplyButton(showButton, buttonLabel);
    }

    /// <summary>
    /// 通用提示（失败、AR 丢失、关卡锁定等）——不使用胜利美术。
    /// </summary>
    public void ShowMessage(string message, bool showButton, string buttonLabel = "继续", Action onClick = null)
    {
        AutoBind();

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (victorySkin != null)
            victorySkin.ShowSkin(false);

        if (messagePanel != null)
            messagePanel.SetActive(true);

        if (resultText != null)
        {
            string msg = message ?? "";
            resultText.text = msg;
            TmpChineseFontUtil.Apply(resultText, msg);
        }

        pendingAction = onClick;
        ApplyButton(showButton, buttonLabel);
    }

    private void ApplyButton(bool showButton, string buttonLabel)
    {
        if (restartButton == null) return;
        restartButton.gameObject.SetActive(showButton);
        if (showButton && restartButtonLabel != null)
        {
            restartButtonLabel.text = buttonLabel ?? "继续";
            TmpChineseFontUtil.Apply(restartButtonLabel, restartButtonLabel.text);
        }
    }

    /// <summary>
    /// 胜利美术已有「战斗胜利」大字，去掉消息里的重复标题行。
    /// </summary>
    private static string SanitizeVictoryDetail(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "";

        string[] lines = message.Replace("\r\n", "\n").Split('\n');
        var kept = new System.Collections.Generic.List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line == "封印成功！" || line == "封印成功" || line == "战斗胜利" || line == "战斗胜利！")
                continue;
            if (line == "三关尽破，封妖成功！" || line == "三关尽破，封妖成功")
            {
                kept.Add(line);
                continue;
            }
            kept.Add(line);
        }
        return string.Join("\n", kept);
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
