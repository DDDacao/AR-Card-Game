using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对局设置：右上角设置图标 → 弹出面板 →「重新开始本关」。
/// 图标资源：Resources/BattleHudSkin/settings_icon
/// </summary>
public class BattleSettingsUI : MonoBehaviour
{
    public static BattleSettingsUI Instance { get; private set; }

    [Header("引用（可空，由搭建菜单绑定）")]
    public Button settingsButton;
    public GameObject settingsButtonRoot;
    public GameObject panelRoot;
    public Button restartStageButton;
    public Button closeButton;
    public Button dimBackgroundButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI restartLabel;

    [Header("可选外部引用")]
    public BattleFlowManager battleFlow;
    public TurnManager turnManager;
    public BattleResultUI resultUI;

    [Header("行为")]
    [Tooltip("未开战时是否隐藏设置按钮")]
    public bool hideButtonWhenNotInBattle = true;
    [Tooltip("打开面板时暂停出牌交互")]
    public bool blockCardsWhenOpen = true;

    private bool panelOpen;
    private float pollTimer;

    private void Awake()
    {
        Instance = this;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        WireButtons();
        RefreshButtonVisibility();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnwireButtons();
    }

    private void Update()
    {
        pollTimer -= Time.unscaledDeltaTime;
        if (pollTimer > 0f) return;
        pollTimer = 0.35f;
        RefreshButtonVisibility();
    }

    private void WireButtons()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(TogglePanel);
            settingsButton.onClick.AddListener(TogglePanel);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
        if (dimBackgroundButton != null)
        {
            dimBackgroundButton.onClick.RemoveListener(ClosePanel);
            dimBackgroundButton.onClick.AddListener(ClosePanel);
        }
        if (restartStageButton != null)
        {
            restartStageButton.onClick.RemoveListener(OnRestartStageClicked);
            restartStageButton.onClick.AddListener(OnRestartStageClicked);
        }
    }

    private void UnwireButtons()
    {
        if (settingsButton != null) settingsButton.onClick.RemoveListener(TogglePanel);
        if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
        if (dimBackgroundButton != null) dimBackgroundButton.onClick.RemoveListener(ClosePanel);
        if (restartStageButton != null) restartStageButton.onClick.RemoveListener(OnRestartStageClicked);
    }

    private void RefreshButtonVisibility()
    {
        if (settingsButtonRoot == null && settingsButton != null)
            settingsButtonRoot = settingsButton.gameObject;

        if (settingsButtonRoot == null) return;

        bool show = true;
        if (hideButtonWhenNotInBattle)
        {
            if (turnManager == null)
                turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();
            show = turnManager != null && turnManager.IsBattleActive && !turnManager.BattleEnded;
        }

        if (settingsButtonRoot.activeSelf != show)
            settingsButtonRoot.SetActive(show);

        // 非战斗时若面板开着则关掉
        if (!show && panelOpen)
            ClosePanel();
    }

    public void TogglePanel()
    {
        if (panelOpen) ClosePanel();
        else OpenPanel();
    }

    public void OpenPanel()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        panelOpen = true;

        if (titleText != null)
        {
            titleText.text = "设置";
            TmpChineseFontUtil.Apply(titleText, titleText.text);
        }
        if (restartLabel != null)
        {
            restartLabel.text = "重新开始本关";
            TmpChineseFontUtil.Apply(restartLabel, restartLabel.text);
        }

        if (blockCardsWhenOpen)
            CardDragHandler.InteractionEnabled = false;
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        panelOpen = false;

        // 仅当战斗进行中且非结果界面时恢复出牌
        if (blockCardsWhenOpen)
        {
            if (turnManager == null)
                turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();
            bool battleOk = turnManager != null && turnManager.IsBattleActive && !turnManager.BattleEnded
                            && turnManager.IsPlayerTurn;
            // 若结果/奖励面板开着，保持锁牌
            bool resultOpen = resultUI != null && resultUI.panelRoot != null && resultUI.panelRoot.activeSelf;
            CardDragHandler.InteractionEnabled = battleOk && !resultOpen;
        }
    }

    public void OnRestartStageClicked()
    {
        Debug.Log("[BattleSettings] 重新开始本关");
        ClosePanel();

        // 关掉胜负/提示面板
        if (resultUI == null)
            resultUI = FindAnyObjectByType<BattleResultUI>(FindObjectsInactive.Include);
        if (resultUI != null)
            resultUI.HidePanel();

        if (battleFlow == null)
            battleFlow = BattleFlowManager.Instance != null
                ? BattleFlowManager.Instance
                : FindAnyObjectByType<BattleFlowManager>();

        if (battleFlow != null)
        {
            battleFlow.RetryCurrentStage();
            CardDragHandler.InteractionEnabled = true;
            return;
        }

        // 无战役流时回退 TurnManager
        if (turnManager == null)
            turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();
        if (turnManager != null)
        {
            turnManager.RestartBattle();
            CardDragHandler.InteractionEnabled = true;
            return;
        }

        Debug.LogWarning("[BattleSettings] 无法重开本关：没有 BattleFlowManager / TurnManager。");
    }
}
