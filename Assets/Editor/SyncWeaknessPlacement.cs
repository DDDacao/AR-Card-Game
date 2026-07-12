using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 把怪物 Prefab 上的弱点手调位置，同步到场景里对应实例。
/// 不依赖 Inspector 的 Overrides 按钮（选中「预制件资产」时本来就没有该按钮）。
/// </summary>
public static class SyncWeaknessPlacement
{
    [MenuItem("AR封妖/同步弱点位置：小妖 Prefab → 场景")]
    public static void SyncXiaoYao()
    {
        int n = SyncFromPrefab(
            "Assets/fbx/monsters/1/Prefabs/Vespomorph.prefab",
            "Vespomorph");
        EditorUtility.DisplayDialog("同步弱点",
            n > 0
                ? $"已把小妖 Prefab 上 {n} 个弱点的位置/父节点同步到场景实例。\n请保存场景后进 Play 验证。"
                : "场景中没找到 Vespomorph 实例，或 Prefab 上没有 WeaknessPoint。",
            "OK");
    }

    [MenuItem("AR封妖/同步弱点位置：石灵 Prefab → 场景")]
    public static void SyncShiLing()
    {
        int n = SyncFromPrefab(
            "Assets/fbx/monsters/2/Prefabs/Cavecrawler.prefab",
            "Cavecrawler");
        EditorUtility.DisplayDialog("同步弱点",
            n > 0
                ? $"已同步石灵 {n} 个弱点到场景。"
                : "场景中没找到 Cavecrawler，或 Prefab 无弱点。",
            "OK");
    }

    [MenuItem("AR封妖/同步弱点位置：山鬼 Prefab → 场景")]
    public static void SyncShanGui()
    {
        int n = SyncFromPrefab(
            "Assets/fbx/monsters/3/Prefabs/Drackmahre.prefab",
            "Drackmahre");
        EditorUtility.DisplayDialog("同步弱点",
            n > 0
                ? $"已同步山鬼 {n} 个弱点到场景。"
                : "场景中没找到 Drackmahre，或 Prefab 无弱点。",
            "OK");
    }

    [MenuItem("AR封妖/同步弱点位置：三关全部 Prefab → 场景")]
    public static void SyncAll()
    {
        int total = 0;
        total += SyncFromPrefab("Assets/fbx/monsters/1/Prefabs/Vespomorph.prefab", "Vespomorph");
        total += SyncFromPrefab("Assets/fbx/monsters/2/Prefabs/Cavecrawler.prefab", "Cavecrawler");
        total += SyncFromPrefab("Assets/fbx/monsters/3/Prefabs/Drackmahre.prefab", "Drackmahre");
        EditorUtility.DisplayDialog("同步弱点", $"三关合计同步 {total} 个弱点节点。", "OK");
    }

    /// <summary>
    /// 按弱点类型 + 名称，把 Prefab 上的 local 变换拷到场景同名怪物实例。
    /// </summary>
    static int SyncFromPrefab(string prefabPath, string monsterRootName)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("[SyncWeakness] 无法加载 Prefab: " + prefabPath);
            return 0;
        }

        var prefabPoints = prefabRoot.GetComponentsInChildren<WeaknessPoint>(true);
        int synced = 0;

        try
        {
            // 场景中所有同名根（含未激活）
            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in allTransforms)
            {
                if (t == null || t.name != monsterRootName) continue;
                if (EditorUtility.IsPersistent(t.gameObject)) continue; // 跳过 Project 资产
                if (t.gameObject.scene.name == null || !t.gameObject.scene.IsValid()) continue;

                foreach (var src in prefabPoints)
                {
                    if (src == null) continue;
                    WeaknessPoint dst = FindMatching(t, src);
                    if (dst == null)
                    {
                        // 场景缺节点：从 Prefab 复制一份
                        dst = CopyPointUnder(t, src);
                    }
                    if (dst == null) continue;

                    ApplyTransformLikePrefab(dst.transform, src.transform, t);
                    dst.keepManualPlacement = true;
                    dst.followTarget = null;
                    dst.weaknessType = src.weaknessType;
                    dst.visualCoreScale = src.visualCoreScale;
                    dst.hitRadius = src.hitRadius;
                    dst.showMarker = src.showMarker;
                    EditorUtility.SetDirty(dst);
                    EditorUtility.SetDirty(dst.gameObject);
                    synced++;
                }
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        if (synced > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"[SyncWeakness] {monsterRootName}: synced {synced} from {prefabPath}");
        return synced;
    }

    static WeaknessPoint FindMatching(Transform monsterRoot, WeaknessPoint src)
    {
        var all = monsterRoot.GetComponentsInChildren<WeaknessPoint>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (all[i].weaknessType == src.weaknessType) return all[i];
            if (all[i].name == src.name) return all[i];
        }
        return null;
    }

    static WeaknessPoint CopyPointUnder(Transform monsterRoot, WeaknessPoint src)
    {
        // 尽量挂到同名骨骼；找不到则挂怪物根
        Transform parent = monsterRoot;
        if (src.transform.parent != null)
        {
            var found = FindDeep(monsterRoot, src.transform.parent.name);
            if (found != null) parent = found;
        }

        var go = new GameObject(src.name);
        go.transform.SetParent(parent, false);
        var wp = go.AddComponent<WeaknessPoint>();
        return wp;
    }

    static void ApplyTransformLikePrefab(Transform dst, Transform src, Transform monsterRoot)
    {
        // 父节点：按 Prefab 父节点名字对齐
        if (src.parent != null)
        {
            var wantParent = FindDeep(monsterRoot, src.parent.name);
            if (wantParent != null && dst.parent != wantParent)
                dst.SetParent(wantParent, false);
        }

        dst.localPosition = src.localPosition;
        dst.localRotation = src.localRotation;
        dst.localScale = src.localScale;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var f = FindDeep(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }
}
