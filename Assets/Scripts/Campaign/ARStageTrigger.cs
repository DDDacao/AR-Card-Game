using UnityEngine;
using Vuforia;

/// <summary>
/// 挂在每个 Vuforia ImageTarget 节点上，监听目标的识别与丢失，并通知 BattleFlowManager
/// </summary>
public class ARStageTrigger : MonoBehaviour
{
    [Header("对应关卡索引 (0:小妖, 1:石灵, 2:山鬼)")]
    public int stageIndex;

    private ObserverBehaviour mObserverBehaviour;
    private CharacterStats mMonsterStats;

    private void Start()
    {
        EnsureMonsterStats();
        mObserverBehaviour = GetComponent<ObserverBehaviour>();
        if (mObserverBehaviour != null)
        {
            mObserverBehaviour.OnTargetStatusChanged += OnStatusChanged;
        }
    }

    private void OnDestroy()
    {
        if (mObserverBehaviour != null)
        {
            mObserverBehaviour.OnTargetStatusChanged -= OnStatusChanged;
        }
    }

    private void EnsureMonsterStats()
    {
        if (mMonsterStats != null) return;

        mMonsterStats = GetComponentInChildren<CharacterStats>(true);
        if (mMonsterStats == null)
        {
            // 运行时兜底：如果在子物体中找不到 CharacterStats，就找 Animator 挂载点（怪物模型根节点）
            Animator childAnim = GetComponentInChildren<Animator>(true);
            if (childAnim != null)
            {
                mMonsterStats = childAnim.gameObject.GetComponent<CharacterStats>();
                if (mMonsterStats == null)
                {
                    mMonsterStats = childAnim.gameObject.AddComponent<CharacterStats>();
                    Debug.Log($"[ARStageTrigger] 动态为 {childAnim.gameObject.name} 挂载 CharacterStats 组件。");
                }
            }
        }
    }

    private void OnStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isTracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
        
        if (isTracked)
        {
            EnsureMonsterStats();
            
            if (BattleFlowManager.Instance != null)
            {
                BattleFlowManager.Instance.OnARCardTracked(stageIndex, mMonsterStats);
            }
        }
        else
        {
            if (BattleFlowManager.Instance != null)
            {
                BattleFlowManager.Instance.OnARCardLost(stageIndex);
            }
        }
    }
}
