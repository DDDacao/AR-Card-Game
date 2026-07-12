using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

        reward.cardBaseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/Cardpng/卡牌底.png");
        if (reward.cardBaseSprite == null)
        {
            Debug.LogError("[RewardCards] 未找到卡牌底图。");
            return;
        }

        EditorUtility.SetDirty(reward);
        EditorSceneManager.MarkSceneDirty(reward.gameObject.scene);
        Debug.Log("[RewardCards] 已绑定正式卡牌底图；奖励将以三张完整卡牌显示。");
    }
}
