using UnityEngine;

/// <summary>
/// 敌人弱点：美术图标 billboard + 外圈光晕呼吸 + SphereCollider 判定。
/// 资源：Resources/WeaknessMarkers/weakness_red|yellow|purple
/// 瞄准高亮；无粒子，避免红色方块 billboard 伪影。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class WeaknessPoint : MonoBehaviour
{
    [Header("弱点类型")]
    public WeaknessType weaknessType = WeaknessType.RedAttack;

    [Header("位置（手调）")]
    [Tooltip("勾选后：开战不再自动重挂头骨/改 localPosition，可在 Prefab 里精确摆放。\n取消勾选：开战时由 WeaknessAnchorSetup 自动贴到头部外侧。")]
    public bool keepManualPlacement = true;

    [Header("显示")]
    public bool showMarker = true;
    [Tooltip("图标在世界空间的大致直径（米）")]
    public float visualCoreScale = 0.38f;
    [Tooltip("外圈光晕相对核心的缩放")]
    [Range(1.05f, 1.8f)]
    public float haloScaleMul = 1.28f;
    [Tooltip("光晕透明度")]
    [Range(0.1f, 0.8f)]
    public float haloAlpha = 0.35f;
    [Tooltip("慢速自转（度/秒），0=不转")]
    public float spinDegreesPerSecond = 12f;
    public Color markerColor = new Color(1f, 0.25f, 0.22f, 0.95f);

    [Header("可选：覆盖默认 Resources 贴图")]
    public Texture2D customIcon;

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
    private Transform haloTf;
    private Renderer coreRenderer;
    private Renderer haloRenderer;
    private Material coreMat;
    private Material haloMat;
    private SphereCollider hitCol;
    private float pulseT;
    private Color baseColor;
    private Color aimColor;
    private Texture2D iconTex;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
    private static readonly int CullId = Shader.PropertyToID("_Cull");

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<CharacterStats>();

        hitCol = GetComponent<SphereCollider>();
        if (hitCol == null)
            hitCol = gameObject.AddComponent<SphereCollider>();
        hitCol.isTrigger = false;
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

    private void OnDestroy()
    {
        if (coreMat != null) Destroy(coreMat);
        if (haloMat != null) Destroy(haloMat);
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

        // Billboard：始终朝向主相机
        Camera cam = Camera.main;
        if (cam == null)
        {
            var cams = Object.FindObjectsByType<Camera>();
            if (cams != null && cams.Length > 0) cam = cams[0];
        }
        if (cam != null)
        {
            Vector3 toCam = visualRoot.transform.position - cam.transform.position;
            if (toCam.sqrMagnitude > 0.0001f)
                visualRoot.transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }

        pulseT += Time.deltaTime;
        float breathe = 1f + Mathf.Sin(pulseT * (IsAimed ? 6.5f : 2.6f)) * (IsAimed ? 0.1f : 0.06f);
        float aimBoost = IsAimed ? (IsTypeMatch ? 1.35f : 1.12f) : 1f;
        float diam = visualCoreScale * breathe * aimBoost;
        ApplyCoreWorldScale(diam);

        // 核心慢转 + 光晕反向慢转，增强「封印阵」感
        if (coreTf != null && Mathf.Abs(spinDegreesPerSecond) > 0.01f)
            coreTf.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime, Space.Self);
        if (haloTf != null && Mathf.Abs(spinDegreesPerSecond) > 0.01f)
            haloTf.Rotate(0f, 0f, -spinDegreesPerSecond * 0.55f * Time.deltaTime, Space.Self);
    }

    public CharacterStats GetOwner()
    {
        if (owner == null)
            owner = GetComponentInParent<CharacterStats>();
        return owner;
    }

    public void SetActiveWeakness(bool active)
    {
        if (gameObject.activeSelf != active)
            gameObject.SetActive(active);

        if (!active)
        {
            SetAimed(false, false);
            if (visualRoot != null)
                visualRoot.SetActive(false);
            if (coreRenderer != null) coreRenderer.enabled = false;
            if (haloRenderer != null) haloRenderer.enabled = false;
            if (hitCol != null) hitCol.enabled = false;
            return;
        }

        if (hitCol != null)
            hitCol.enabled = true;
        if (showMarker)
        {
            EnsureVisual();
            if (visualRoot != null) visualRoot.SetActive(true);
            if (coreRenderer != null) coreRenderer.enabled = true;
            if (haloRenderer != null) haloRenderer.enabled = true;
            ApplyVisualState(false, false);
        }
    }

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
                baseColor = new Color(1f, 0.28f, 0.22f, 1f);
                aimColor = new Color(1f, 0.55f, 0.4f, 1f);
                break;
            case WeaknessType.YellowArmor:
                baseColor = new Color(1f, 0.86f, 0.28f, 1f);
                aimColor = new Color(1f, 0.96f, 0.55f, 1f);
                break;
            case WeaknessType.PurpleSeal:
                baseColor = new Color(0.78f, 0.4f, 1f, 1f);
                aimColor = new Color(0.92f, 0.62f, 1f, 1f);
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

        // 清理旧版纯色球（若场景里残留）
        var old = transform.Find("WeaknessVisual");
        if (old != null)
            Destroy(old.gameObject);

        visualRoot = new GameObject("WeaknessVisual");
        visualRoot.transform.SetParent(transform, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;

        iconTex = customIcon != null ? customIcon : LoadIconTexture(weaknessType);

        // 外圈光晕（略大、更淡、反向转）
        var halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        halo.name = "Halo";
        halo.transform.SetParent(visualRoot.transform, false);
        halo.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        haloTf = halo.transform;
        DestroyCollider(halo);
        haloRenderer = halo.GetComponent<Renderer>();
        haloMat = CreateIconMaterial(iconTex, baseColor, haloAlpha * 0.85f, true);
        if (haloRenderer != null)
        {
            haloRenderer.sharedMaterial = haloMat;
            haloRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            haloRenderer.receiveShadows = false;
        }

        // 主图标
        var core = GameObject.CreatePrimitive(PrimitiveType.Quad);
        core.name = "Core";
        core.transform.SetParent(visualRoot.transform, false);
        core.transform.localPosition = Vector3.zero;
        coreTf = core.transform;
        DestroyCollider(core);
        coreRenderer = core.GetComponent<Renderer>();
        coreMat = CreateIconMaterial(iconTex, Color.white, 1f, false);
        if (coreRenderer != null)
        {
            coreRenderer.sharedMaterial = coreMat;
            coreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coreRenderer.receiveShadows = false;
        }

        ApplyCoreWorldScale(visualCoreScale);
        ApplyVisualState(false, false);
    }

    private static void DestroyCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null)
            Destroy(col);
    }

    private static Texture2D LoadIconTexture(WeaknessType type)
    {
        string id = type switch
        {
            WeaknessType.YellowArmor => "weakness_yellow",
            WeaknessType.PurpleSeal => "weakness_purple",
            _ => "weakness_red"
        };
        var tex = Resources.Load<Texture2D>("WeaknessMarkers/" + id);
        if (tex == null)
            tex = Resources.Load<Texture2D>("BattleHudSkin/" + id);
        return tex;
    }

    private void ApplyCoreWorldScale(float worldDiameter)
    {
        float lossy = Mathf.Max(0.0001f, transform.lossyScale.x);
        float local = worldDiameter / lossy;
        if (coreTf != null)
            coreTf.localScale = Vector3.one * local;
        if (haloTf != null)
            haloTf.localScale = Vector3.one * (local * haloScaleMul);
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

        // 主图标保持贴图本色，瞄准时提亮；光晕跟类型色
        Color coreTint = aimed
            ? (typeMatch ? Color.Lerp(Color.white, aimColor, 0.35f) : Color.Lerp(Color.white, Color.gray, 0.15f))
            : Color.white;
        float coreEmit = aimed ? (typeMatch ? 2.2f : 1.3f) : 1.05f;
        SetMaterialColor(coreMat, coreTint, coreEmit);

        if (haloMat != null)
        {
            Color hc = aimed ? (typeMatch ? aimColor : baseColor) : baseColor;
            hc.a = aimed ? Mathf.Min(0.75f, haloAlpha + 0.25f) : haloAlpha;
            SetMaterialColor(haloMat, hc, aimed ? 1.6f : 1.0f);
        }
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

    private static Material CreateIconMaterial(Texture2D tex, Color tint, float alpha, bool additiveLook)
    {
        // 优先 URP Unlit 透明；失败则 Sprites/Default
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Transparent");
        if (sh == null) sh = Shader.Find("Unlit/Color");

        var mat = new Material(sh);
        mat.name = additiveLook ? "WeaknessHalo" : "WeaknessIcon";

        Color c = tint;
        c.a = alpha;

        if (tex != null)
        {
            if (mat.HasProperty(BaseMapId))
                mat.SetTexture(BaseMapId, tex);
            if (mat.HasProperty(MainTexId))
                mat.SetTexture(MainTexId, tex);
            mat.mainTexture = tex;
        }

        // URP Unlit 透明设置
        if (mat.HasProperty(SurfaceId))
            mat.SetFloat(SurfaceId, 1f); // Transparent
        if (mat.HasProperty(BlendId))
            mat.SetFloat(BlendId, additiveLook ? 1f : 0f); // 1=Additive-ish / Alpha
        if (mat.HasProperty(SrcBlendId))
            mat.SetFloat(SrcBlendId, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty(DstBlendId))
            mat.SetFloat(DstBlendId, additiveLook
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty(ZWriteId))
            mat.SetFloat(ZWriteId, 0f);
        if (mat.HasProperty(CullId))
            mat.SetFloat(CullId, 0f); // Off — 双面

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        SetMaterialColor(mat, c, 1.1f);
        return mat;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Color c = markerColor;
        c.a = 0.15f;
        Gizmos.color = c;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
        c.a = 0.35f;
        Gizmos.color = c;
        Gizmos.DrawWireSphere(transform.position, visualCoreScale * 0.5f);
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
