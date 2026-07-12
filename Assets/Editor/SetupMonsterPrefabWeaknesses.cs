using UnityEditor;
using UnityEngine;

/// <summary>
/// 把三色弱点 SphereCollider + WeaknessPoint 写进怪物 Prefab 头部，方便在 Hierarchy 里直接调。
/// 菜单：AR封妖 / 配置怪物Prefab弱点（头部）
/// </summary>
public static class SetupMonsterPrefabWeaknesses
{
    static readonly (string path, string[] headNames)[] Targets =
    {
        ("Assets/fbx/monsters/1/Prefabs/Vespomorph.prefab", new[] { "Vespomorph_Head", "Head", "head" }),
        ("Assets/fbx/monsters/2/Prefabs/Cavecrawler.prefab", new[] { "CAVECRAWLER_HEAD", "Head", "head" }),
        // 注意：FBX 骨骼名是 "Drackmahre_ Head"（下划线后有空格）
        ("Assets/fbx/monsters/3/Prefabs/Drackmahre.prefab", new[] { "Drackmahre_ Head", "Drackmahre_Head", "Head", "head" }),
    };

    [MenuItem("AR封妖/配置怪物Prefab弱点（头部）")]
    public static void SetupAllMenu()
    {
        int ok = SetupAll(showDialog: true);
        Debug.Log($"[SetupMonsterPrefabWeaknesses] 完成 {ok}/{Targets.Length}");
    }

    /// <summary>供其它 Editor 菜单调用；返回成功写入的 Prefab 数。</summary>
    public static int SetupAll(bool showDialog = false)
    {
        int ok = 0;
        for (int i = 0; i < Targets.Length; i++)
        {
            if (SetupOne(Targets[i].path, Targets[i].headNames))
                ok++;
        }

        // 场景 Ellen 上的旧弱点关掉，避免和 Prefab 重复
        DisableSceneLegacyWeaknesses();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "配置怪物Prefab弱点",
                $"已处理 {ok}/{Targets.Length} 个怪物 Prefab。\n\n" +
                "• 红/黄/紫弱点挂在头部骨骼下\n" +
                "• 仅小球 + SphereCollider（无粒子）\n" +
                "• 场景 Ellen 旧弱点已禁用\n\n" +
                "可在 Prefab 里直接拖偏移、改 hitRadius。",
                "OK");
        }

        return ok;
    }

    static bool SetupOne(string prefabPath, string[] headNames)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogError($"[SetupMonsterPrefabWeaknesses] 无法加载: {prefabPath}");
            return false;
        }

        try
        {
            // 清掉已有弱点
            var olds = root.GetComponentsInChildren<WeaknessPoint>(true);
            for (int i = 0; i < olds.Length; i++)
            {
                if (olds[i] != null)
                    Object.DestroyImmediate(olds[i].gameObject);
            }

            Transform head = FindDeep(root.transform, headNames);
            if (head == null)
            {
                Debug.LogWarning($"[SetupMonsterPrefabWeaknesses] {prefabPath} 未找到头部，挂到根节点。");
                head = root.transform;
            }

            // 局部偏移尽量往外，运行时 AnchorSetup 还会再按世界方向推出
            CreateWeakness(head, "Weakness_Red", WeaknessType.RedAttack, new Vector3(0f, 0.25f, 0.4f));
            CreateWeakness(head, "Weakness_Yellow", WeaknessType.YellowArmor, new Vector3(0.15f, 0.2f, 0.4f));
            CreateWeakness(head, "Weakness_Purple", WeaknessType.PurpleSeal, new Vector3(-0.15f, 0.2f, 0.4f));

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[SetupMonsterPrefabWeaknesses] 已写入 {prefabPath} → head={head.name}");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void CreateWeakness(Transform head, string name, WeaknessType type, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(head, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var sc = go.AddComponent<SphereCollider>();
        sc.radius = 0.65f;
        sc.isTrigger = false;
        sc.center = Vector3.zero;

        var wp = go.AddComponent<WeaknessPoint>();
        wp.weaknessType = type;
        wp.showMarker = true;
        wp.visualCoreScale = 0.28f;
        wp.hitRadius = 0.65f;
        wp.markerColor = type switch
        {
            WeaknessType.RedAttack => new Color(1f, 0.22f, 0.18f, 0.95f),
            WeaknessType.YellowArmor => new Color(1f, 0.82f, 0.2f, 0.95f),
            WeaknessType.PurpleSeal => new Color(0.72f, 0.32f, 1f, 0.95f),
            _ => Color.white
        };
        wp.followTarget = null;
        wp.followLocalOffset = Vector3.zero;
        wp.owner = null; // 运行时由 CharacterStats 向上找 / AnchorSetup 赋值
        wp.keepManualPlacement = true; // Prefab 里可手调位置，开战不再覆盖
    }

    static void DisableSceneLegacyWeaknesses()
    {
        var enemy = GameObject.Find("Ellen_skin (2)");
        if (enemy == null) return;

        for (int i = 0; i < enemy.transform.childCount; i++)
        {
            var child = enemy.transform.GetChild(i);
            if (child == null) continue;
            if (child.name.StartsWith("Weakness_") || child.GetComponent<WeaknessPoint>() != null)
            {
                child.gameObject.SetActive(false);
                EditorUtility.SetDirty(child.gameObject);
            }
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    }

    static Transform FindDeep(Transform root, string[] names)
    {
        for (int n = 0; n < names.Length; n++)
        {
            var t = FindRecursive(root, names[n]);
            if (t != null) return t;
        }

        // 兜底：名字包含 Head
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return all[i];
        }
        return null;
    }

    static Transform FindRecursive(Transform root, string name)
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
