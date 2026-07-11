using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 创建破煞/镇魂卡、三色弱点、意图控制器，并更新牌库。
/// 菜单：AR封妖 / 配置多弱点与新符咒
/// </summary>
public static class SetupMultiWeakness
{
    const string CardDataFolder = "Assets/Game Data/Card Data";
    const string AttackPath = "Assets/Game Data/Card Data/attack.asset";
    const string LibraryPath = "Assets/Game Data/Card Library/NewGameLibrary.asset";

    [MenuItem("AR封妖/配置多弱点与新符咒")]
    public static void Setup()
    {
        // ---- 卡牌 SO ----
        var attack = AssetDatabase.LoadAssetAtPath<CardDataSO>(AttackPath);
        if (attack == null)
        {
            EditorUtility.DisplayDialog("配置", "找不到 attack.asset", "OK");
            return;
        }

        // 确保攻击牌标签
        attack.cardName = "斩妖符";
        attack.cardType = CardType.Attack;
        attack.weaknessTag = WeaknessType.RedAttack;
        attack.effectValue = 6;
        attack.cost = 1;
        attack.description = "造成6点伤害；命中红破绽可QTE";
        EditorUtility.SetDirty(attack);

        var defense = AssetDatabase.LoadAssetAtPath<CardDataSO>(CardDataFolder + "/defense.asset");
        if (defense != null)
        {
            defense.cardName = "护身符";
            defense.cardType = CardType.Defense;
            defense.effectValue = 5;
            defense.cost = 1;
            defense.description = "获得5点护甲";
            EditorUtility.SetDirty(defense);
        }

        var qi = AssetDatabase.LoadAssetAtPath<CardDataSO>(CardDataFolder + "/hp.asset");
        if (qi != null)
        {
            qi.cardName = "聚气诀";
            qi.cardType = CardType.Ability;
            qi.effectValue = 1;
            qi.cost = 0;
            qi.description = "回复1点灵气";
            EditorUtility.SetDirty(qi);
        }

        var breakCard = CreateOrUpdateCard("break", "破煞符", CardType.ArmorBreak, WeaknessType.YellowArmor,
            4, 4, 1, "造成4点伤害并破甲；命中黄裂纹可QTE", attack.cardImage);
        var sealCard = CreateOrUpdateCard("seal", "镇魂符", CardType.Seal, WeaknessType.PurpleSeal,
            6, 0, 2, "造成6点伤害；命中紫封印QTE可打断蓄力", attack.cardImage);

        // ---- 牌库 ----
        var lib = AssetDatabase.LoadAssetAtPath<CardLibrarySO>(LibraryPath);
        if (lib != null)
        {
            lib.cardLibraryList.Clear();
            lib.cardLibraryList.Add(new CardLibraryEntry { cardData = attack, amount = 4 });
            if (defense != null)
                lib.cardLibraryList.Add(new CardLibraryEntry { cardData = defense, amount = 2 });
            lib.cardLibraryList.Add(new CardLibraryEntry { cardData = breakCard, amount = 2 });
            lib.cardLibraryList.Add(new CardLibraryEntry { cardData = sealCard, amount = 1 });
            if (qi != null)
                lib.cardLibraryList.Add(new CardLibraryEntry { cardData = qi, amount = 1 });
            EditorUtility.SetDirty(lib);
        }

        // ---- 敌人弱点 + 意图 ----
        GameObject enemy = GameObject.Find("Ellen_skin (2)");
        if (enemy == null)
        {
            EditorUtility.DisplayDialog("配置", "卡牌已创建，但找不到 Ellen_skin (2)，请手动挂弱点。", "OK");
            AssetDatabase.SaveAssets();
            return;
        }

        // 清旧弱点
        var olds = enemy.GetComponentsInChildren<WeaknessPoint>(true);
        for (int i = 0; i < olds.Length; i++)
            Object.DestroyImmediate(olds[i].gameObject);

        CreateWeakness(enemy, "Weakness_Red", WeaknessType.RedAttack, new Vector3(0f, 1.2f, 0.35f), 0.42f);
        CreateWeakness(enemy, "Weakness_Yellow", WeaknessType.YellowArmor, new Vector3(0.35f, 0.95f, 0.3f), 0.42f);
        CreateWeakness(enemy, "Weakness_Purple", WeaknessType.PurpleSeal, new Vector3(-0.3f, 1.35f, 0.3f), 0.42f);

        var intent = enemy.GetComponent<EnemyIntentController>();
        if (intent == null) intent = enemy.AddComponent<EnemyIntentController>();
        intent.stats = enemy.GetComponent<CharacterStats>();
        intent.RefreshWeaknessList();
        intent.ResetIntent();

        var tm = Object.FindAnyObjectByType<TurnManager>();
        if (tm != null)
        {
            tm.enemyStats = enemy.GetComponent<CharacterStats>();
            tm.enemyIntent = intent;
            EditorUtility.SetDirty(tm);
        }

        // 确保 QTE 仍在
        if (Object.FindAnyObjectByType<QTEManager>() == null)
        {
            SetupWeaknessAndQTE.Setup();
        }

        EditorUtility.SetDirty(enemy);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SetupMultiWeakness] 完成：破煞/镇魂卡 + 三色弱点 + 意图循环。");
        EditorUtility.DisplayDialog("配置多弱点与新符咒",
            "已完成：\n" +
            "• 斩妖×4 护身×2 破煞×2 镇魂×1 聚气×1\n" +
            "• 红/黄/紫三弱点\n" +
            "• 意图循环：攻击→防御→蓄力\n\n" +
            "玩法：看意图颜色，用对应符打对应弱点触发 QTE。",
            "OK");
    }

    static CardDataSO CreateOrUpdateCard(string fileName, string cardName, CardType type, WeaknessType tag,
        int effect, int effect2, int cost, string desc, Sprite icon)
    {
        string path = $"{CardDataFolder}/{fileName}.asset";
        var so = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<CardDataSO>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.cardName = cardName;
        so.cardType = type;
        so.weaknessTag = tag;
        so.effectValue = effect;
        so.effectValue2 = effect2;
        so.cost = cost;
        so.description = desc;
        if (icon != null) so.cardImage = icon;
        EditorUtility.SetDirty(so);
        return so;
    }

    static void CreateWeakness(GameObject enemy, string name, WeaknessType type, Vector3 localPos, float radius)
    {
        var go = new GameObject(name);
        go.transform.SetParent(enemy.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one;
        var sc = go.AddComponent<SphereCollider>();
        sc.radius = radius;
        sc.isTrigger = false;
        var wp = go.AddComponent<WeaknessPoint>();
        wp.weaknessType = type;
        wp.visualCoreScale = 0.1f;
        wp.hitRadius = 0.62f;
        wp.owner = enemy.GetComponent<CharacterStats>();
        wp.showMarker = true;
    }
}
