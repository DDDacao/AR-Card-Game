using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 奖励三选一改为直接使用手牌 Card 预制体。
/// 菜单：AR封妖 / 配置奖励三选一卡牌界面
/// </summary>
public static class SetupRewardCardPresentation
{
    [MenuItem("AR封妖/配置奖励三选一卡牌界面")]
    public static void Configure()
    {
        RewardSelectUI reward = Object.FindAnyObjectByType<RewardSelectUI>(FindObjectsInactive.Include);
        if (reward == null)
        {
            Debug.LogError("[RewardCards] 未找到 RewardSelectUI。");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Card/Card.prefab");
        reward.cardPrefab = prefab;
        reward.displayScale = 0.55f;
        reward.worldSpacing = 2.4f;
        reward.viewLocalCenter = new Vector3(0f, 0.15f, 7.5f);

        EditorUtility.SetDirty(reward);
        EditorSceneManager.MarkSceneDirty(reward.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[RewardCards] 已绑定手牌 Card 预制体。奖励界面将实例化真实卡牌，不再 UI 重拼。");
    }
}
