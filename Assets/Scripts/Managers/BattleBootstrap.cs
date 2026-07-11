using UnityEngine;

/// <summary>
/// 控制战斗何时开始：Editor 可跳过 AR；真机可在识别后调用 BeginBattle。
/// 不硬依赖 Vuforia 类型，避免编译顺序/包引用问题。
/// </summary>
public class BattleBootstrap : MonoBehaviour
{
    [Header("Editor 调试")]
    [Tooltip("在编辑器中跳过 AR 识别，直接开战")]
    public bool skipARForEditor = true;

    [Header("引用（可空，自动查找）")]
    public TurnManager turnManager;
    public GameObject enemyVisualRoot;

    [Header("真机：无 AR 回调时也可手动开战")]
    public bool autoStartIfNoAR = true;

    private bool battleStarted;

    private void Start()
    {
        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

#if UNITY_EDITOR
        if (skipARForEditor)
        {
            Debug.Log("[BattleBootstrap] Editor 跳过 AR，直接开战。");
            BeginBattle();
            return;
        }
#endif
        if (autoStartIfNoAR)
        {
            // Demo：无专门 AR 事件绑定时，延迟一帧开战（真机识别脚本可改为只调 BeginBattle）
            Debug.Log("[BattleBootstrap] 自动开战（可在 ImageTarget 识别回调中改为只调用 BeginBattle）。");
            BeginBattle();
        }
        else
        {
            Debug.Log("[BattleBootstrap] 等待外部调用 BeginBattle()…");
        }
    }

    public void BeginBattle()
    {
        if (battleStarted) return;
        battleStarted = true;

        if (enemyVisualRoot != null)
            enemyVisualRoot.SetActive(true);

        if (turnManager == null)
            turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();

        // 优先走战役流程（三关）
        var flow = BattleFlowManager.Instance != null
            ? BattleFlowManager.Instance
            : FindAnyObjectByType<BattleFlowManager>();
        if (flow != null && flow.campaign != null)
        {
            flow.StartCampaign();
            return;
        }

        if (turnManager != null)
        {
            turnManager.StartBattle();
        }
        else
        {
            Debug.LogError("[BattleBootstrap] 找不到 TurnManager。");
        }
    }

    public void ResetAndStart()
    {
        battleStarted = false;
        BeginBattle();
    }
}
