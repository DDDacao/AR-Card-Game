using UnityEngine;

/// <summary>
/// 按关卡把弱点锚到怪物骨骼（先做第 1 关小妖翅膀）。
/// 由 BattleFlowManager 换模后调用。
/// </summary>
public static class WeaknessAnchorSetup
{
    /// <summary>
    /// stageIndex: 0 小妖 / 1 石灵 / 2 山鬼
    /// </summary>
    public static void ApplyForStage(GameObject enemyRoot, GameObject activeMonster, int stageIndex)
    {
        if (enemyRoot == null) return;

        var points = enemyRoot.GetComponentsInChildren<WeaknessPoint>(true);
        if (points == null || points.Length == 0) return;

        // 默认：大判定、小视觉（所有关统一手感）
        for (int i = 0; i < points.Length; i++)
        {
            var wp = points[i];
            if (wp == null) continue;
            wp.hitRadius = 0.62f;
            wp.visualCoreScale = 0.1f;
            wp.glowParticleSize = 0.16f;
            var col = wp.GetComponent<SphereCollider>();
            if (col != null) col.radius = wp.hitRadius;
        }

        if (stageIndex == 0)
            SetupXiaoYao(enemyRoot, activeMonster, points);
        // 其它关卡后续再做
    }

    private static void SetupXiaoYao(GameObject enemyRoot, GameObject activeMonster, WeaknessPoint[] points)
    {
        // 红弱点 → 右翅（第一回合攻击意图）
        Transform wing = FindDeep(activeMonster != null ? activeMonster.transform : enemyRoot.transform,
            "Vespomorph_WingRightA", "WingRightA", "SK_VespomorphWingRightA");

        WeaknessPoint red = null;
        WeaknessPoint yellow = null;
        WeaknessPoint purple = null;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            switch (points[i].weaknessType)
            {
                case WeaknessType.RedAttack: red = points[i]; break;
                case WeaknessType.YellowArmor: yellow = points[i]; break;
                case WeaknessType.PurpleSeal: purple = points[i]; break;
            }
        }

        if (red != null)
        {
            // 跟随翅膀中段偏外，略向前方便射线点中
            if (wing != null)
            {
                red.BindFollow(wing, new Vector3(-0.35f, 0.05f, 0.12f));
                Debug.Log($"[WeaknessAnchor] 红弱点跟随翅膀: {wing.name}");
            }
            else
            {
                // 回退：相对 Ellen 本地，翅膀大致高度
                red.followTarget = null;
                red.transform.SetParent(enemyRoot.transform, false);
                red.transform.localPosition = new Vector3(0.55f, 1.05f, 0.25f);
                Debug.LogWarning("[WeaknessAnchor] 未找到翅膀骨骼，红弱点用近似坐标。");
            }
            red.hitRadius = 0.68f; // 第一关红弱点更好点
            var col = red.GetComponent<SphereCollider>();
            if (col != null) col.radius = red.hitRadius;
        }

        // 黄 / 紫：先摆在身体合理位置（暂不绑骨，后续关再精修）
        if (yellow != null)
        {
            yellow.followTarget = null;
            yellow.transform.SetParent(enemyRoot.transform, false);
            yellow.transform.localPosition = new Vector3(0f, 0.75f, 0.35f);
        }
        if (purple != null)
        {
            purple.followTarget = null;
            purple.transform.SetParent(enemyRoot.transform, false);
            purple.transform.localPosition = new Vector3(-0.15f, 1.35f, 0.2f);
        }

        var intent = enemyRoot.GetComponent<EnemyIntentController>();
        if (intent != null)
        {
            intent.RefreshWeaknessList();
            intent.RefreshWeaknessVisibility();
        }
    }

    private static Transform FindDeep(Transform root, params string[] names)
    {
        if (root == null) return null;
        for (int n = 0; n < names.Length; n++)
        {
            var t = FindRecursive(root, names[n]);
            if (t != null) return t;
        }
        return null;
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var f = FindRecursive(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }
}
