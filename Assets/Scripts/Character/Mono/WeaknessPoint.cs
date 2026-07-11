using UnityEngine;

/// <summary>
/// 敌人弱点：小而亮的光球（+ 粒子光晕），大范围碰撞便于瞄准。
/// 支持瞄准高亮、跟随骨骼。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class WeaknessPoint : MonoBehaviour
{
    [Header("弱点类型")]
    public WeaknessType weaknessType = WeaknessType.RedAttack;

    [Header("显示")]
    public bool showMarker = true;
    [Tooltip("光芯视觉大小（本地）")]
    public float visualCoreScale = 0.11f;
    [Tooltip("光晕粒子大小")]
    public float glowParticleSize = 0.18f;
    public Color markerColor = new Color(1f, 0.25f, 0.22f, 0.9f);

    [Header("判定（比看起来大，好点中）")]
    [Tooltip("SphereCollider 半径，世界空间随父级缩放")]
    public float hitRadius = 0.55f;

    [Header("跟随（可选，挂到翅膀骨骼）")]
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
    private ParticleSystem glowPs;
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
        hitCol.radius = hitRadius;
        hitCol.center = Vector3.zero;
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
        if (hitCol != null)
            hitCol.radius = hitRadius;
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
            // 保持世界朝向稳定，避免骨骼旋转把球扭扁观感
            transform.rotation = Quaternion.identity;
        }

        if (visualRoot == null || !visualRoot.activeInHierarchy) return;

        // 轻柔呼吸脉动
        pulseT += Time.deltaTime;
        float breathe = 1f + Mathf.Sin(pulseT * (IsAimed ? 7f : 3.2f)) * (IsAimed ? 0.12f : 0.05f);
        float aimBoost = IsAimed ? (IsTypeMatch ? 1.45f : 1.15f) : 1f;
        if (coreTf != null)
            coreTf.localScale = Vector3.one * visualCoreScale * breathe * aimBoost;
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
                baseColor = new Color(1f, 0.22f, 0.18f, 0.92f);
                aimColor = new Color(1f, 0.45f, 0.35f, 1f);
                break;
            case WeaknessType.YellowArmor:
                baseColor = new Color(1f, 0.82f, 0.2f, 0.92f);
                aimColor = new Color(1f, 0.95f, 0.45f, 1f);
                break;
            case WeaknessType.PurpleSeal:
                baseColor = new Color(0.72f, 0.32f, 1f, 0.92f);
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

        // —— 光芯：小半透明球 + 自发光 ——
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Core";
        core.transform.SetParent(visualRoot.transform, false);
        core.transform.localPosition = Vector3.zero;
        core.transform.localScale = Vector3.one * visualCoreScale;
        coreTf = core.transform;
        var coreCol = core.GetComponent<Collider>();
        if (coreCol != null) Destroy(coreCol);

        coreRenderer = core.GetComponent<Renderer>();
        coreMat = CreateGlowMaterial(baseColor);
        if (coreRenderer != null)
            coreRenderer.sharedMaterial = coreMat;

        // —— 粒子光晕 ——
        var psGo = new GameObject("GlowParticles");
        psGo.transform.SetParent(visualRoot.transform, false);
        glowPs = psGo.AddComponent<ParticleSystem>();
        ConfigureGlowParticles(glowPs, baseColor);

        var psr = psGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            var pmat = CreateParticleMaterial(baseColor);
            psr.sharedMaterial = pmat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }

    private void ApplyVisualState(bool aimed, bool typeMatch)
    {
        if (coreMat == null && visualRoot != null)
            EnsureVisual();
        if (coreMat == null) return;

        Color c = aimed ? (typeMatch ? aimColor : Color.Lerp(baseColor, Color.white, 0.25f)) : baseColor;
        float emit = aimed ? (typeMatch ? 3.2f : 1.6f) : 1.1f;
        SetMaterialColor(coreMat, c, emit);

        if (glowPs != null)
        {
            var main = glowPs.main;
            main.startColor = new Color(c.r, c.g, c.b, aimed ? 0.55f : 0.28f);
            var emission = glowPs.emission;
            emission.rateOverTime = aimed ? (typeMatch ? 28f : 16f) : 10f;
            if (!glowPs.isPlaying) glowPs.Play();
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

    private static Material CreateGlowMaterial(Color color)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh);
        mat.name = "WeaknessGlowCore";

        // 尽量半透明 + 自发光
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SrcBlend"))
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))
            mat.SetInt("_ZWrite", 0);
        if (mat.HasProperty("_Cull"))
            mat.SetInt("_Cull", 0);

        SetMaterialColor(mat, color, 1.2f);
        return mat;
    }

    private static Material CreateParticleMaterial(Color color)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh);
        mat.name = "WeaknessGlowParticle";
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        mat.color = color;
        // Additive-ish
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        return mat;
    }

    private void ConfigureGlowParticles(ParticleSystem ps, Color color)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 1.2f;
        main.startSize = glowParticleSize;
        main.startColor = new Color(color.r, color.g, color.b, 0.3f);
        main.startSpeed = 0.02f;
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = 10f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.02f;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));

        var colOver = ps.colorOverLifetime;
        colOver.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.45f, 0.25f),
                new GradientAlphaKey(0f, 1f)
            });
        colOver.color = grad;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.4f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Color c = markerColor;
        c.a = 0.2f;
        Gizmos.color = c;
        Gizmos.DrawWireSphere(transform.position, hitRadius * Mathf.Max(transform.lossyScale.x, 0.01f));
        c.a = 0.7f;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, visualCoreScale * 0.5f);
    }

    private void OnValidate()
    {
        var col = GetComponent<SphereCollider>();
        if (col != null)
            col.radius = hitRadius;
    }
#endif
}
