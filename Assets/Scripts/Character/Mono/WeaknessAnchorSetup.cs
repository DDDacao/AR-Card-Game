using UnityEngine;

/// <summary>
/// 换模后：关闭场景旧弱点，启用怪物 Prefab 上的弱点，并统一挂到头部便于测试。
/// 由 BattleFlowManager.SwapMonsterModel 调用。
/// </summary>
public static class WeaknessAnchorSetup
{
    /// <summary>
    /// stageIndex: 0 小妖 / 1 石灵 / 2 山鬼
    /// </summary>
    public static void ApplyForStage(GameObject enemyRoot, GameObject activeMonster, int stageIndex)
    {
        if (enemyRoot == null) return;

        // 场景 Ellen 上的旧 Weakness_* 不再使用，避免重复判定/红色方块
        DisableLegacySceneWeaknesses(enemyRoot);

        CharacterStats stats = enemyRoot.GetComponent<CharacterStats>();
        Transform searchRoot = activeMonster != null ? activeMonster.transform : enemyRoot.transform;

        WeaknessPoint[] points = searchRoot.GetComponentsInChildren<WeaknessPoint>(true);
        if (points == null || points.Length == 0)
        {
            // Prefab 尚未配置时运行时兜底：在头部生成三色弱点
            CreateHeadWeaknesses(searchRoot.gameObject, stats);
            points = searchRoot.GetComponentsInChildren<WeaknessPoint>(true);
        }

        Transform head = FindHeadBone(searchRoot, stageIndex);

        for (int i = 0; i < points.Length; i++)
        {
            WeaknessPoint wp = points[i];
            if (wp == null) continue;

            wp.owner = stats;
            wp.showMarker = true;

            var col = wp.GetComponent<SphereCollider>();
            if (col != null)
            {
                col.isTrigger = false;
                col.center = Vector3.zero;
            }

            // 手调模式：完全保留 Prefab/场景里的父节点与 localPosition / 尺寸
            if (wp.keepManualPlacement)
            {
                // 仍清掉 follow，避免 LateUpdate 再拽走手摆位置
                wp.followTarget = null;
                continue;
            }

            // 自动模式：重挂头骨并推到外侧
            wp.hitRadius = 0.65f;
            wp.visualCoreScale = 0.42f;
            wp.followTarget = null;

            if (head != null)
            {
                if (wp.transform.parent != head)
                    wp.transform.SetParent(head, false);
                PlaceOutsideHead(wp.transform, head, wp.weaknessType);
            }
            else
            {
                wp.transform.localPosition = LocalOffsetForType(wp.weaknessType);
                wp.transform.localRotation = Quaternion.identity;
                wp.transform.localScale = Vector3.one;
            }
        }

        var intent = enemyRoot.GetComponent<EnemyIntentController>();
        if (intent != null)
        {
            intent.RefreshWeaknessList();
            intent.RefreshWeaknessVisibility();
        }

        Debug.Log($"[WeaknessAnchor] stage={stageIndex} head={(head != null ? head.name : "null")} points={points.Length}");
    }

    private static void DisableLegacySceneWeaknesses(GameObject enemyRoot)
    {
        // 只关 Ellen 根下直接挂的旧弱点，不动 ActiveMonster 内的
        for (int i = 0; i < enemyRoot.transform.childCount; i++)
        {
            Transform child = enemyRoot.transform.GetChild(i);
            if (child == null) continue;
            if (child.name == "ActiveMonster") continue;

            var wps = child.GetComponents<WeaknessPoint>();
            if (wps != null && wps.Length > 0)
            {
                child.gameObject.SetActive(false);
                continue;
            }

            // 名字匹配兜底
            if (child.name.StartsWith("Weakness_"))
                child.gameObject.SetActive(false);
        }
    }

    private static void CreateHeadWeaknesses(GameObject monsterRoot, CharacterStats owner)
    {
        CreateOne(monsterRoot, "Weakness_Red", WeaknessType.RedAttack, owner);
        CreateOne(monsterRoot, "Weakness_Yellow", WeaknessType.YellowArmor, owner);
        CreateOne(monsterRoot, "Weakness_Purple", WeaknessType.PurpleSeal, owner);
    }

    private static void CreateOne(GameObject parent, string name, WeaknessType type, CharacterStats owner)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = LocalOffsetForType(type);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var sc = go.AddComponent<SphereCollider>();
        sc.radius = 0.55f;
        sc.isTrigger = false;

        var wp = go.AddComponent<WeaknessPoint>();
        wp.weaknessType = type;
        wp.visualCoreScale = 0.42f;
        wp.hitRadius = 0.65f;
        wp.owner = owner;
        wp.showMarker = true;
        // 运行时临时生成的点先自动摆位；你可在 Prefab 里改好后勾 keepManualPlacement
        wp.keepManualPlacement = false;
    }

    /// <summary>
    /// 把头前/上方向推出弱点，避免陷在网格里。三色微偏便于 Prefab 辨认。
    /// </summary>
    private static void PlaceOutsideHead(Transform wp, Transform head, WeaknessType type)
    {
        // 侧向微偏（头骨局部 X）
        float side = type switch
        {
            WeaknessType.YellowArmor => 0.12f,
            WeaknessType.PurpleSeal => -0.12f,
            _ => 0f
        };

        // 世界空间：上 + 朝玩家大致前方（用 head.forward；若怪异再用 up 叉乘兜底）
        Vector3 up = Vector3.up;
        Vector3 forward = head.forward;
        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.85f)
            forward = head.right; // 头骨 forward 几乎朝上时换轴
        forward = Vector3.ProjectOnPlane(forward, up).normalized;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        Vector3 world = head.position
                        + up * 0.32f
                        + forward * 0.42f
                        + head.right * side;

        wp.SetParent(head, true);
        wp.position = world;
        wp.rotation = Quaternion.identity;
        wp.localScale = Vector3.one;
    }

    private static Vector3 LocalOffsetForType(WeaknessType type)
    {
        // 无头骨时的回退（相对怪物根）
        switch (type)
        {
            case WeaknessType.RedAttack:
                return new Vector3(0f, 1.4f, 0.45f);
            case WeaknessType.YellowArmor:
                return new Vector3(0.15f, 1.35f, 0.45f);
            case WeaknessType.PurpleSeal:
                return new Vector3(-0.15f, 1.35f, 0.45f);
            default:
                return new Vector3(0f, 1.4f, 0.45f);
        }
    }

    private static Transform FindHeadBone(Transform root, int stageIndex)
    {
        if (root == null) return null;

        string[] names = stageIndex switch
        {
            0 => new[] { "Vespomorph_Head", "Head", "head" },
            1 => new[] { "CAVECRAWLER_HEAD", "Head", "head" },
            // FBX 骨骼名是 "Drackmahre_ Head"（下划线后有空格）
            2 => new[] { "Drackmahre_ Head", "Drackmahre_Head", "Head", "head" },
            _ => new[] { "Head", "head" }
        };

        Transform found = FindDeep(root, names);
        if (found != null) return found;

        // 通用兜底：名字包含 Head（兼容 "Drackmahre_ Head" 等带空格骨骼）
        Transform loose = FindNameContains(root, "Head");
        if (loose != null) return loose;
        return FindDeep(root, "Head", "head", "HEAD", "Skull", "skull");
    }

    private static Transform FindNameContains(Transform root, string token)
    {
        if (root == null || string.IsNullOrEmpty(token)) return null;
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return all[i];
        }
        return null;
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
