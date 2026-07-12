using UnityEngine;

/// <summary>
/// 敌人弱点：只有一个小光球 + SphereCollider 判定。
/// 不挂粒子，避免出现红色方块 billboard。
/// 支持瞄准高亮；父级缩放变化时自动补偿视觉与碰撞半径。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class WeaknessPoint : MonoBehaviour
{
    [Header("弱点类型")]
    public WeaknessType weaknessType = WeaknessType.RedAttack;

    [Header("显示")]
    public bool showMarker = true;
    [Tooltip("光球在世界空间的大致直径（米）")]
    public float visualCoreScale = 0.12f;
    public Color markerColor = new Color(1f, 0.25f, 0.22f, 0.95f);

    [Header("判定（比看起来大，好点中；世界空间半径）")]
    [Tooltip("SphereCollider 世界空间半径")]
    public float hitRadius = 0.55f;

    [Header("跟随（可选；若已挂在头骨下可不填）")]
    public Transform followTarget;
    public Vector3 followLocalOffset = Vector3.zero;

    [Header("所属敌人（可空，自动向上找）")]
    public CharacterStats owner;

    [Header("运行时状态（只读）")]
    public bool IsAimed { get; private set; }
    public bool IsTypeMatch { get; private set; }

    private GameObject visualRoot;
    private Transform coreTf;
    private Renderer coreRenderer;
    private Material coreMat;
    private SphereCollider hitCol;
    private float pulseT;
    private Color baseColor;
    private Color aimColor;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<CharacterStats>();

        hitCol = GetComponent<SphereCollider>();
        if (hitCol == null)
            hitCol = gameObject.AddComponent<SphereCollider>();
        hitCol.isTrigger = false; // Raycast 需要
        hitCol.center = Vector3.zero;
        ApplyColliderRadius();
    }

    private void Start()
    {
        ApplyTypeColor();
        if (showMarker)
            EnsureVisual();
        ApplyVisualState(false, false);
    }

    private void OnEnable()
    {
        ApplyTypeColor();
        if (showMarker && visualRoot == null)
            EnsureVisual();
        if (visualRoot != null)
            visualRoot.SetActive(true);
        ApplyColliderRadius();
    }

    private void OnDisable()
    {
        SetAimed(false, false);
    }

    private void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.TransformPoint(followLocalOffset);
            transform.rotation = Quaternion.identity;
        }

        ApplyColliderRadius();

        if (visualRoot == null || !visualRoot.activeInHierarchy) return;

        pulseT += Time.deltaTime;
        float breathe = 1f + Mathf.Sin(pulseT * (IsAimed ? 7f : 3.2f)) * (IsAimed ? 0.12f : 0.05f);
        float aimBoost = IsAimed ? (IsTypeMatch ? 1.45f : 1.15f) : 1f;
        ApplyCoreWorldScale(visualCoreScale * breathe * aimBoost);
    }

    public CharacterStats GetOwner()
    {
        if (owner == null)
            owner = GetComponentInParent<CharacterStats>();
        return owner;
    }

    public void SetActiveWeakness(bool active)
    {
        gameObject.SetActive(active);
        if (!active)
            SetAimed(false, false);
        else if (showMarker)
        {
            EnsureVisual();
            if (visualRoot != null) visualRoot.SetActive(true);
            ApplyVisualState(false, false);
        }
    }

    /// <summary>瞄准时调用：typeMatch=卡牌标签与弱点一致时更亮。</summary>
    public void SetAimed(bool aimed, bool typeMatch)
    {
        if (IsAimed == aimed && IsTypeMatch == typeMatch) return;
        IsAimed = aimed;
        IsTypeMatch = typeMatch;
        ApplyVisualState(aimed, typeMatch);
    }

    public void BindFollow(Transform target, Vector3 localOffset)
    {
        followTarget = target;
        followLocalOffset = localOffset;
        if (followTarget != null)
        {
            transform.position = followTarget.TransformPoint(followLocalOffset);
            transform.rotation = Quaternion.identity;
        }
    }

    private void ApplyTypeColor()
    {
        switch (weaknessType)
        {
            case WeaknessType.RedAttack:
                baseColor = new Color(1f, 0.22f, 0.18f, 0.95f);
                aimColor = new Color(1f, 0.55f, 0.4f, 1f);
                break;
            case WeaknessType.YellowArmor:
                baseColor = new Color(1f, 0.82f, 0.2f, 0.95f);
                aimColor = new Color(1f, 0.95f, 0.5f, 1f);
                break;
            case WeaknessType.PurpleSeal:
                baseColor = new Color(0.72f, 0.32f, 1f, 0.95f);
                aimColor = new Color(0.9f, 0.55f, 1f, 1f);
                break;
            default:
                baseColor = markerColor;
                aimColor = Color.Lerp(markerColor, Color.white, 0.4f);
                break;
        }
        markerColor = baseColor;
    }

    private void EnsureVisual()
    {
        if (visualRoot != null) return;

        visualRoot = new GameObject("WeaknessVisual");
        visualRoot.transform.SetParent(transform, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;

        // 仅一个小球：去掉粒子，避免出现红色方块 billboard
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Core";
        core.transform.SetParent(visualRoot.transform, false);
        core.transform.localPosition = Vector3.zero;
        coreTf = core.transform;

        // 去掉默认 MeshCollider，判定只用父物体 SphereCollider
        var coreCol = core.GetComponent<Collider>();
        if (coreCol != null)
            Destroy(coreCol);

        coreRenderer = core.GetComponent<Renderer>();
        coreMat = CreateBallMaterial(baseColor);
        if (coreRenderer != null)
        {
            coreRenderer.sharedMaterial = coreMat;
            coreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coreRenderer.receiveShadows = false;
        }

        ApplyCoreWorldScale(visualCoreScale);
    }

    private void ApplyCoreWorldScale(float worldDiameter)
    {
        if (coreTf == null) return;
        float lossy = Mathf.Max(0.0001f, transform.lossyScale.x);
        float local = worldDiameter / lossy;
        coreTf.localScale = Vector3.one * local;
    }

    private void ApplyColliderRadius()
    {
        if (hitCol == null) return;
        float lossy = Mathf.Max(0.0001f, transform.lossyScale.x);
        hitCol.radius = hitRadius / lossy;
        hitCol.center = Vector3.zero;
    }

    private void ApplyVisualState(bool aimed, bool typeMatch)
    {
        if (coreMat == null && showMarker)
            EnsureVisual();
        if (coreMat == null) return;

        Color c = aimed ? (typeMatch ? aimColor : Color.Lerp(baseColor, Color.white, 0.25f)) : baseColor;
        float emit = aimed ? (typeMatch ? 2.8f : 1.5f) : 1.0f;
        SetMaterialColor(coreMat, c, emit);
    }

    private static void SetMaterialColor(Material mat, Color c, float emissionMul)
    {
        if (mat == null) return;
        if (mat.HasProperty(BaseColorId))
            mat.SetColor(BaseColorId, c);
        if (mat.HasProperty(ColorId))
            mat.SetColor(ColorId, c);
        mat.color = c;
        if (mat.HasProperty(EmissionColorId))
        {
            mat.EnableKeyword("_EMISSION");
            Color e = c * emissionMul;
            e.a = 1f;
            mat.SetColor(EmissionColorId, e);
        }
    }

    /// <summary>
    /// 用不带纹理的 Unlit 实心球材质，避免 URP Lit 透明/粒子方块伪影。
    /// </summary>
    private static Material CreateBallMaterial(Color color)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Standard");

        var mat = new Material(sh);
        mat.name = "WeaknessBall";
        SetMaterialColor(mat, color, 1.1f);
        return mat;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Color c = markerColor;
        c.a = 0.15f;
        Gizmos.color = c;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
        c.a = 0.75f;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, visualCoreScale * 0.5f);
    }

    private void OnValidate()
    {
        var col = GetComponent<SphereCollider>();
        if (col == null) return;
        float lossy = 1f;
        if (Application.isPlaying)
            lossy = Mathf.Max(0.0001f, transform.lossyScale.x);
        col.radius = hitRadius / lossy;
        col.center = Vector3.zero;
    }
#endif
}
